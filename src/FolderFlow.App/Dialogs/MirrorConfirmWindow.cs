using System.Windows;
using System.Windows.Controls;

namespace FolderFlow.App.Dialogs;

public sealed class MirrorConfirmWindow : Window
{
    public MirrorConfirmWindow(string source, string destination)
    {
        DialogUi.Apply(this, "Confirm mirror · FolderFlow", 580, 500);
        var body = new StackPanel();
        body.Children.Add(DialogUi.Text("Make these folders match?", size: 25));
        body.Children.Add(DialogUi.Text("Files in the destination that are missing from the source will be deleted. Changed files in the destination may be replaced."));
        var sourceLabel = DialogUi.Text("Copy from", muted: true);
        sourceLabel.Margin = new Thickness(0, 17, 0, 5);
        body.Children.Add(sourceLabel);
        body.Children.Add(PathText(source));
        var targetLabel = DialogUi.Text("Mirror into", muted: true);
        targetLabel.Margin = new Thickness(0, 17, 0, 5);
        body.Children.Add(targetLabel);
        body.Children.Add(PathText(destination));
        body.Children.Add(DialogUi.Text("Check the destination carefully. Deleted files may not be recoverable.", muted: true));
        var agreement = new CheckBox
        {
            Content = DialogUi.Text("I understand files may be deleted from the destination."),
            Margin = new Thickness(0, 18, 0, 0),
            VerticalContentAlignment = VerticalAlignment.Top
        };
        body.Children.Add(agreement);
        var cancel = DialogUi.Button("Go back");
        cancel.IsCancel = true;
        var confirm = DialogUi.Button("Mirror folders", primary: true);
        confirm.IsEnabled = false;
        agreement.Checked += (_, _) => confirm.IsEnabled = true;
        agreement.Unchecked += (_, _) => confirm.IsEnabled = false;
        confirm.Click += (_, _) => DialogResult = true;
        Content = DialogUi.Frame(new ScrollViewer { Content = body, VerticalScrollBarVisibility = ScrollBarVisibility.Auto },
            DialogUi.Actions(cancel, confirm));
    }

    private static TextBox PathText(string path)
    {
        var text = new TextBox
        {
            Text = path,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 10)
        };
        text.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush");
        text.SetResourceReference(Control.ForegroundProperty, "TextBrush");
        return text;
    }
}
