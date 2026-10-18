using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FolderFlow.Core;

namespace FolderFlow.App.Dialogs;

public sealed class DetailsWindow : Window
{
    public DetailsWindow(CopyResult result)
    {
        DialogUi.Apply(this, "Copy details · FolderFlow", 760, 560);
        var body = new Grid();
        body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        body.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        body.Children.Add(DialogUi.Text("Copy details", size: 25));
        string details = result.Details + Environment.NewLine + Environment.NewLine +
            $"RoboCopy exit code: {result.ExitCode?.ToString() ?? "Not available"}" + Environment.NewLine +
            $"Log: {result.LogPath ?? "Not saved"}";
        var text = new TextBox
        {
            Text = details,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 12, 0, 0)
        };
        text.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, "Copy diagnostic details");
        text.SetResourceReference(Control.BackgroundProperty, "SurfaceBrush");
        text.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        text.SetResourceReference(Control.BorderBrushProperty, "BorderBrush");
        Grid.SetRow(text, 1);
        body.Children.Add(text);

        var copy = DialogUi.Button("Copy details");
        copy.Click += (_, _) =>
        {
            try { Clipboard.SetText(details); }
            catch (System.Runtime.InteropServices.COMException)
            { MessageBox.Show(this, "The clipboard is busy. Please try again.", "Copy details", MessageBoxButton.OK, MessageBoxImage.Information); }
        };
        var open = DialogUi.Button("Open log");
        open.IsEnabled = result.LogPath is not null && File.Exists(result.LogPath);
        open.Click += (_, _) =>
        {
            try
            {
                if (result.LogPath is null || !File.Exists(result.LogPath)) throw new FileNotFoundException();
                Process.Start(new ProcessStartInfo(result.LogPath) { UseShellExecute = true });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or InvalidOperationException)
            { MessageBox.Show(this, "The log could not be opened. It may have been moved or removed.", "Open log", MessageBoxButton.OK, MessageBoxImage.Information); }
        };
        var close = DialogUi.Button("Close", primary: true);
        close.IsCancel = true;
        close.Click += (_, _) => Close();
        Content = DialogUi.Frame(body, DialogUi.Actions(copy, open, close));
    }
}
