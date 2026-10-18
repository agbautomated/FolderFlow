using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using FolderFlow.Core;
using FolderFlow.App.Dialogs;
using FolderFlow.App.Services;

namespace FolderFlow.App;

public partial class MainWindow : Window
{
    private Preferences _preferences;
    private CancellationTokenSource? _cancellation;
    private CopyResult? _result;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Stopwatch _elapsed = new();
    private bool _running, _preparing, _closeAfterStop, _dryRun, _mirror;
    private string? _preferenceWarning;

    public MainWindow()
    {
        _preferences = PreferenceStore.Load(out var warning);
        InitializeComponent();
        App.ApplyTheme(_preferences.Theme);
        SourceBox.Text = _preferences.SourcePath;
        DestinationBox.Text = _preferences.DestinationPath;
        _timer.Tick += (_, _) => UpdateElapsed(_elapsed.Elapsed);
        if (warning is not null) ShowError(warning);
        UpdateStartEnabled();
    }

    private void ChooseSource_Click(object sender, RoutedEventArgs e) => ChooseFolder(SourceBox, "Choose the folder to copy from");
    private void ChooseDestination_Click(object sender, RoutedEventArgs e) => ChooseFolder(DestinationBox, "Choose the folder to copy to");

    private void ChooseFolder(TextBox box, string title)
    {
        var dialog = new OpenFolderDialog { Title = title, Multiselect = false };
        if (Directory.Exists(box.Text)) dialog.InitialDirectory = box.Text;
        if (dialog.ShowDialog(this) == true) box.Text = dialog.FolderName;
    }

    private void Path_Changed(object sender, TextChangedEventArgs e)
    {
        if (StartButton is null) return;
        ErrorPanel.Visibility = Visibility.Collapsed;
        UpdateStartEnabled();
    }

    private void UpdateStartEnabled()
    {
        bool ready = !_preparing && !_running && !string.IsNullOrWhiteSpace(SourceBox.Text) && !string.IsNullOrWhiteSpace(DestinationBox.Text);
        StartButton.IsEnabled = ready;
        PreviewButton.IsEnabled = ready;
    }

    private async void Start_Click(object sender, RoutedEventArgs e) => await RunJobAsync(false);
    private async void Preview_Click(object sender, RoutedEventArgs e) => await RunJobAsync(true);

    private async Task RunJobAsync(bool dryRun)
    {
        if (_running || _preparing) return;
        _preparing = true;
        SetupView.IsEnabled = false;
        StartButton.IsEnabled = PreviewButton.IsEnabled = false;
        SettingsButton.IsEnabled = false;
        _cancellation = new CancellationTokenSource();
        ErrorPanel.Visibility = Visibility.Collapsed;
        var job = new CopyJob(SourceBox.Text.Trim(), DestinationBox.Text.Trim(),
            MirrorChoice.IsChecked == true ? CopyMode.Mirror : CopyMode.Copy, _preferences.CopySettings, dryRun);
        try
        {
            // Resolve aliases, junctions and protected paths before any process is started.
            var validated = await Task.Run(() => PathValidator.Validate(job), _cancellation.Token);
            _cancellation.Token.ThrowIfCancellationRequested();
            job = job with { SourcePath = validated.SourcePath, DestinationPath = validated.DestinationPath };
            if (!dryRun && job.Mode == CopyMode.Mirror)
            {
                var confirmation = new MirrorConfirmWindow(job.SourcePath, job.DestinationPath) { Owner = this };
                if (confirmation.ShowDialog() != true) return;
            }
            _preferences = _preferences with { SourcePath = job.SourcePath, DestinationPath = job.DestinationPath };
            PreferenceStore.Save(_preferences, out _preferenceWarning);
            _preparing = false;
            _running = true;
            _dryRun = dryRun;
            _mirror = job.Mode == CopyMode.Mirror;
            _result = null;
            SetupView.Visibility = Visibility.Collapsed;
            ProgressView.Visibility = Visibility.Visible;
            StatusHeading.Text = "Preparing";
            StatusMessage.Text = dryRun ? "Checking what would change. Your files will stay as they are." : "Keep both drives connected until the transfer finishes.";
            RouteText.Text = $"{job.SourcePath}\n→  {job.DestinationPath}";
            FileLabel.Text = "Reading folders…";
            FileLabel.ToolTip = null;
            ProgressNote.Text = "Windows is working. File counts appear when it finishes.";
            CopiedCount.Text = SkippedCount.Text = FailedCount.Text = "—";
            CopiedLabel.Text = dryRun ? "files to copy" : "files copied";
            StatusGlyph.Text = "↗";
            LogNote.Text = _preferenceWarning ?? "";
            ActivityBar.IsIndeterminate = true;
            ActivityBar.Visibility = Visibility.Visible;
            StopButton.Visibility = Visibility.Visible;
            StopButton.IsEnabled = true;
            StopButton.Content = "Stop";
            DoneButton.Visibility = DetailsButton.Visibility = Visibility.Collapsed;
            _elapsed.Restart();
            UpdateElapsed(TimeSpan.Zero);
            _timer.Start();
            var progress = new Progress<CopyProgress>(p =>
            {
                if (!_running || _cancellation?.IsCancellationRequested == true) return;
                StatusHeading.Text = dryRun || p.Operation == "Preparing" ? "Preparing" : "Copying";
                if (!string.IsNullOrWhiteSpace(p.FilePath))
                {
                    FileLabel.Text = p.FilePath;
                    FileLabel.ToolTip = p.FilePath;
                }
                // /MT interleaves file output; a file percentage is not overall progress.
                if (p.FilesCopied.HasValue && !dryRun) CopiedCount.Text = p.FilesCopied.Value.ToString("N0");
                if (p.FilesFailed.HasValue) FailedCount.Text = p.FilesFailed.Value.ToString("N0");
            });
            _result = await new RobocopyRunner().RunAsync(job, progress, _cancellation.Token);
            ShowResult(_result);
        }
        catch (OperationCanceledException) { }
        catch (JobValidationException ex) { ShowError(ex.Message); }
        catch (Exception ex)
        {
            if (_running)
            {
                _result = new CopyResult(CopyOutcome.Failed, null, null, _elapsed.Elapsed, null, ex.ToString(),
                    "We couldn’t finish this transfer. Check that both drives are connected and try again.");
                ShowResult(_result);
            }
            else ShowError("We couldn’t check those folders. Check that both drives are connected and choose them again.");
        }
        finally
        {
            _timer.Stop();
            _elapsed.Stop();
            _running = false;
            _preparing = false;
            _cancellation?.Dispose();
            _cancellation = null;
            SettingsButton.IsEnabled = true;
            SetupView.IsEnabled = true;
            UpdateStartEnabled();
            if (_closeAfterStop) Close();
        }
    }

    private void ShowResult(CopyResult result)
    {
        _timer.Stop();
        _elapsed.Stop();
        // Queued Progress<T> callbacks cannot overwrite the final state.
        _running = false;
        bool success = result.Outcome == CopyOutcome.Completed;
        StatusHeading.Text = success ? "Completed" : "Needs Attention";
        StatusGlyph.Text = success ? "✓" : result.Outcome == CopyOutcome.Cancelled ? "Ⅱ" : "!";
        StatusMessage.Text = result.FriendlyMessage;
        ActivityBar.IsIndeterminate = false;
        ActivityBar.Visibility = Visibility.Collapsed;
        FileLabel.Text = _dryRun ? "Preview finished. No files were copied or deleted." :
            result.Outcome == CopyOutcome.Cancelled ? _mirror ? "Some files may already have been copied, replaced, or deleted." : "Some files may already have been copied or replaced." :
            "Your original source files stay in place.";
        FileLabel.ToolTip = null;
        ProgressNote.Text = result.Files is null
            ? "Windows did not return complete file counts. View Details for more information."
            : _dryRun ? "These are planned changes, not completed copies." : "Reported by Windows for this transfer.";
        if (result.Files is { } f)
        {
            CopiedCount.Text = f.Copied.ToString("N0");
            SkippedCount.Text = f.Skipped.ToString("N0");
            FailedCount.Text = f.Failed.ToString("N0");
        }
        UpdateElapsed(result.Duration);
        LogNote.Text = string.Join(" ", new[] { _preferenceWarning,
            result.LogPath is null ? null : "A log was saved. You can open it in View Details." }.Where(s => !string.IsNullOrEmpty(s)));
        StopButton.Visibility = Visibility.Collapsed;
        DoneButton.Visibility = DetailsButton.Visibility = Visibility.Visible;
        DoneButton.Focus();
    }

    private void UpdateElapsed(TimeSpan elapsed) => ElapsedText.Text =
        $"Elapsed  {(elapsed.TotalHours >= 1 ? elapsed.ToString(@"h\:mm\:ss") : elapsed.ToString(@"m\:ss"))}";

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        StopButton.IsEnabled = false;
        StopButton.Content = "Stopping…";
        StatusMessage.Text = _dryRun ? "Stopping the preview. Your files will stay as they are." : _mirror
            ? "Stopping the transfer. Files may already have been copied or deleted."
            : "Stopping the transfer. Files already copied will stay in place.";
        _cancellation?.Cancel();
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        ProgressView.Visibility = Visibility.Collapsed;
        SetupView.Visibility = Visibility.Visible;
        CopyChoice.IsChecked = true; // Destructive mode is never carried into the next job.
        ErrorPanel.Visibility = Visibility.Collapsed;
        StartButton.Focus();
    }

    private void Details_Click(object sender, RoutedEventArgs e)
    {
        if (_result is not null) new DetailsWindow(_result) { Owner = this }.ShowDialog();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_preferences) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        _preferences = dialog.Result;
        App.ApplyTheme(_preferences.Theme);
        if (!PreferenceStore.Save(_preferences, out var warning) && warning is not null) ShowError(warning);
    }

    private void ShowError(string message)
    {
        if (ProgressView.Visibility == Visibility.Visible)
        {
            LogNote.Text = message;
            return;
        }
        ErrorText.Text = message;
        ErrorPanel.Visibility = Visibility.Visible;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_running && !_preparing) return;
        e.Cancel = true;
        if (_closeAfterStop) return;
        if (MessageBox.Show(this, _mirror && !_dryRun
            ? "Stop the transfer and close FolderFlow? Some files may already have been copied or deleted."
            : "Stop the transfer and close FolderFlow? Files already copied will stay in place.",
            "Transfer in progress", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        _closeAfterStop = true;
        Stop_Click(this, new RoutedEventArgs());
    }
}
