using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FolderFlow.App.Dialogs;

internal static class DialogUi
{
    public static void Apply(Window window, string title, double width, double height)
    {
        window.Title = title;
        window.Width = width;
        window.Height = height;
        window.MinWidth = Math.Min(width, 430);
        window.MinHeight = Math.Min(height, 410);
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.FontFamily = new FontFamily("Segoe UI");
        window.FontSize = 14;
        window.SetResourceReference(Control.BackgroundProperty, "BackgroundBrush");
        window.SetResourceReference(Control.ForegroundProperty, "TextBrush");
    }

    public static TextBlock Text(string text, bool muted = false, double size = 14)
    {
        var block = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontSize = size,
            Margin = new Thickness(0, 0, 0, 8)
        };
        block.SetResourceReference(TextBlock.ForegroundProperty, muted ? "MutedBrush" : "TextBrush");
        return block;
    }

    public static Button Button(string text, bool primary = false)
    {
        var button = new Button
        {
            Content = text,
            Padding = new Thickness(17, 10, 17, 10),
            MinWidth = 88,
            Margin = new Thickness(8, 0, 0, 0)
        };
        button.SetResourceReference(Control.BackgroundProperty, primary ? "AccentBrush" : "SurfaceBrush");
        button.SetResourceReference(Control.ForegroundProperty, primary ? "AccentTextBrush" : "TextBrush");
        button.SetResourceReference(Control.BorderBrushProperty, primary ? "AccentBrush" : "BorderBrush");
        return button;
    }

    public static StackPanel Actions(params Button[] buttons)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 22, 0, 0)
        };
        foreach (var button in buttons) panel.Children.Add(button);
        return panel;
    }

    public static Grid Frame(UIElement content, UIElement actions)
    {
        var frame = new Grid { Margin = new Thickness(28) };
        frame.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        frame.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        frame.Children.Add(content);
        Grid.SetRow(actions, 1);
        frame.Children.Add(actions);
        return frame;
    }
}
