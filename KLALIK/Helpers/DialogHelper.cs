using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace KLALIK.Helpers;

public static class DialogHelper
{
    public static async Task<bool> ConfirmAsync(Window owner, string message, string title)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 440,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var confirmed = false;
        var yes = new Button { Content = "Да", MinWidth = 96, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        var no = new Button { Content = "Нет", MinWidth = 96 };
        yes.Click += (_, _) =>
        {
            confirmed = true;
            dialog.Close();
        };
        no.Click += (_, _) =>
        {
            confirmed = false;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { no, yes }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return confirmed;
    }

    public static async Task<string?> PromptAsync(Window owner, string title, string label, string initial = "")
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        string? result = null;
        var input = new TextBox { Text = initial, PlaceholderText = label, Margin = new Avalonia.Thickness(0, 0, 0, 12) };
        var ok = new Button { Content = "ОК", MinWidth = 96, Margin = new Avalonia.Thickness(8, 0, 0, 0) };
        var cancel = new Button { Content = "Отмена", MinWidth = 96 };
        ok.Click += (_, _) =>
        {
            result = input.Text;
            dialog.Close();
        };
        cancel.Click += (_, _) =>
        {
            result = null;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = label },
                input,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, ok }
                }
            }
        };

        await dialog.ShowDialog(owner);
        return result;
    }

    public static async Task AlertAsync(Window owner, string message, string title)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 420,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var ok = new Button { Content = "ОК", MinWidth = 96, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => dialog.Close();
        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                ok
            }
        };

        await dialog.ShowDialog(owner);
    }
}
