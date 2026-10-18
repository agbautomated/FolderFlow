using System.Windows;
using System.Windows.Controls;
using FolderFlow.App.Services;
using FolderFlow.Core;

namespace FolderFlow.App.Dialogs;

public sealed class SettingsWindow : Window
{
    public Preferences Result { get; private set; }

    public SettingsWindow(Preferences current)
    {
        Result = current;
        DialogUi.Apply(this, "Settings · FolderFlow", 540, 730);
        var body = new StackPanel();
        body.Children.Add(DialogUi.Text("Make it yours", size: 25));
        body.Children.Add(DialogUi.Text("Simple choices for everyday copying.", muted: true));

        body.Children.Add(Heading("Appearance"));
        var theme = new ComboBox { ItemsSource = new[] { "System", "Light", "Dark" }, SelectedItem = current.Theme,
            MinHeight = 35, Margin = new Thickness(0, 0, 0, 10) };
        theme.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, "Appearance");
        body.Children.Add(theme);
        body.Children.Add(DialogUi.Text("System follows your Windows app color setting.", muted: true));

        body.Children.Add(Heading("Copying"));
        var multiple = Option("Copy a few files at once", current.CopySettings.Multithreaded);
        body.Children.Add(multiple);
        body.Children.Add(DialogUi.Text("Uses two workers. A gentle starting point for a USB hard drive.", muted: true));
        var large = Option("Optimize for large files", current.CopySettings.LargeFiles);
        body.Children.Add(large);
        body.Children.Add(DialogUi.Text("Try this for videos, disk images, or other very large files. It may not help small files.", muted: true));
        var log = Option("Save a detailed copy log", current.CopySettings.SaveLog);
        body.Children.Add(log);
        body.Children.Add(DialogUi.Text("Useful if something needs attention. Open the log from View Details.", muted: true));

        body.Children.Add(Heading("If a file cannot be copied"));
        var retries = new ComboBox { MinHeight = 35, Margin = new Thickness(0, 0, 0, 8) };
        foreach (int count in new[] { 0, 2, 5 })
        {
            var item = new ComboBoxItem { Content = count == 0 ? "Do not retry" : $"Try again {count} times", Tag = count };
            retries.Items.Add(item);
            if (current.CopySettings.RetryCount == count) retries.SelectedItem = item;
        }
        if (retries.SelectedIndex < 0) retries.SelectedIndex = 1;
        retries.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, "Retry behavior");
        body.Children.Add(retries);
        body.Children.Add(DialogUi.Text("Waits two seconds between attempts, then continues with the other files.", muted: true));

        body.Children.Add(Heading("Help and privacy"));
        var about = DialogUi.Button("About FolderFlow");
        about.HorizontalAlignment = HorizontalAlignment.Left;
        about.Margin = new Thickness(0, 0, 0, 5);
        about.Click += (_, _) => new AboutWindow { Owner = this }.ShowDialog();
        body.Children.Add(about);
        body.Children.Add(DialogUi.Text("Version, local data, privacy, and support.", muted: true));

        var cancel = DialogUi.Button("Cancel");
        cancel.IsCancel = true;
        var save = DialogUi.Button("Save", primary: true);
        save.IsDefault = true;
        save.Click += (_, _) =>
        {
            Result = current with
            {
                Theme = theme.SelectedItem as string ?? "System",
                CopySettings = new CopySettings
                {
                    Multithreaded = multiple.IsChecked == true,
                    ThreadCount = 2,
                    LargeFiles = large.IsChecked == true,
                    SaveLog = log.IsChecked == true,
                    RetryCount = (int)((ComboBoxItem)retries.SelectedItem).Tag,
                    RetryWaitSeconds = 2
                }
            };
            DialogResult = true;
        };
        Content = DialogUi.Frame(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            DialogUi.Actions(cancel, save));
    }

    private static TextBlock Heading(string text)
    {
        var block = DialogUi.Text(text);
        block.FontWeight = FontWeights.SemiBold;
        block.Margin = new Thickness(0, 16, 0, 9);
        return block;
    }

    private static CheckBox Option(string text, bool selected) => new()
    {
        Content = text,
        IsChecked = selected,
        Margin = new Thickness(0, 7, 0, 7)
    };
}
