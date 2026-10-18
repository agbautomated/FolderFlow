using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using FolderFlow.App.Services;

namespace FolderFlow.App.Dialogs;

public sealed class AboutWindow : Window
{
    private const string SupportUrl = "https://github.com/agbautomated/FolderFlow/issues";
    private const string PrivacyUrl = "https://github.com/agbautomated/FolderFlow/blob/main/docs/PRIVACY.md";

    public AboutWindow()
    {
        DialogUi.Apply(this, "About · FolderFlow", 560, 650);
        var body = new StackPanel();
        body.Children.Add(DialogUi.Text("FolderFlow", size: 25));
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        body.Children.Add(DialogUi.Text($"Version {version?.ToString(3) ?? "1.0.0"}", muted: true));
        body.Children.Add(DialogUi.Text("Careful folder copying with Windows RoboCopy."));

        body.Children.Add(Heading("Privacy"));
        body.Children.Add(DialogUi.Text("FolderFlow does not collect analytics, create an account, or upload your files. It works with the folders you choose on connected drives."));
        body.Children.Add(DialogUi.Text("Your settings remember your selected folder paths and copy preferences. Detailed logs can include file names, folder paths, and Windows error messages. They stay on this computer unless you choose to share them."));
        body.Children.Add(DialogUi.Text("Turn off “Save a detailed copy log” in Settings to avoid keeping new logs. A temporary transcript is still used during a transfer. Cleanup is attempted afterward; a crash or permission problem can leave it behind.", muted: true));
        body.Children.Add(DialogUi.Text("Microsoft Store purchases and Windows diagnostics are handled by Microsoft under its own policies. Opening Support uses your browser and GitHub's policies.", muted: true));

        body.Children.Add(Heading("Local settings and logs"));
        var dataPath = new TextBox
        {
            Text = PreferenceStore.DataDirectory,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6),
            Margin = new Thickness(0, 0, 0, 10)
        };
        dataPath.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, "FolderFlow local data folder");
        dataPath.SetResourceReference(Control.BackgroundProperty, "SurfaceBrush");
        dataPath.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        body.Children.Add(dataPath);
        body.Children.Add(DialogUi.Text("Keep private file names and paths out of public support reports. Copied files remain in the destination you selected.", muted: true));

        var support = DialogUi.Button("Support");
        support.Click += (_, _) => OpenWebsite(SupportUrl);
        var privacy = DialogUi.Button("Privacy policy");
        privacy.Click += (_, _) => OpenWebsite(PrivacyUrl);
        var close = DialogUi.Button("Close", primary: true);
        close.IsCancel = true;
        close.IsDefault = true;
        close.Click += (_, _) => Close();
        Content = DialogUi.Frame(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            DialogUi.Actions(privacy, support, close));
    }

    private void OpenWebsite(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(this, $"Your browser could not be opened. Visit:\n{url}",
                "FolderFlow", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static TextBlock Heading(string text)
    {
        var heading = DialogUi.Text(text);
        heading.FontWeight = FontWeights.SemiBold;
        heading.Margin = new Thickness(0, 16, 0, 9);
        return heading;
    }
}
