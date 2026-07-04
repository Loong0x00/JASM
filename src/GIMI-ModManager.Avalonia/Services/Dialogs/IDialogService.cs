using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Layout;
using global::Avalonia.Media;
using global::Avalonia.Threading;
using FluentAvalonia.UI.Controls;

namespace GIMI_ModManager.Avalonia.Services.Dialogs;

public interface IDialogService
{
    Task ShowMessageAsync(string title, string message, string closeText = "确定");

    /// <summary>Returns true if the user picked the primary action.</summary>
    Task<bool> ShowConfirmAsync(string title, string message, string primaryText = "确定",
        string closeText = "取消");

    /// <summary>Shows an arbitrary control as dialog content; returns true on primary.</summary>
    Task<bool> ShowContentAsync(string title, object content, string primaryText = "确定",
        string closeText = "取消");

    /// <summary>Prompts the user to pick one item from a list; returns the choice or default on cancel.</summary>
    Task<T?> ShowPickerAsync<T>(string title, IReadOnlyList<T> items, Func<T, string> display,
        string? placeholder = null);

    /// <summary>Prompts the user for a single line of text; returns null on cancel.</summary>
    Task<string?> ShowInputAsync(string title, string? initialValue = null, string? placeholder = null);
}

public class DialogService : IDialogService
{
    public Task ShowMessageAsync(string title, string message, string closeText = "确定") =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                CloseButtonText = closeText
            };
            await dialog.ShowAsync();
        });

    public Task<bool> ShowConfirmAsync(string title, string message, string primaryText = "确定",
        string closeText = "取消") =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                PrimaryButtonText = primaryText,
                CloseButtonText = closeText,
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        });

    public Task<bool> ShowContentAsync(string title, object content, string primaryText = "确定",
        string closeText = "取消") =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryText,
                CloseButtonText = closeText,
                DefaultButton = ContentDialogButton.Primary
            };
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        });

    public Task<T?> ShowPickerAsync<T>(string title, IReadOnlyList<T> items, Func<T, string> display,
        string? placeholder = null) =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var options = items.Select(i => new PickerOption<T>(i, display(i))).ToList();
            var combo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = options,
                PlaceholderText = placeholder ?? "请选择…",
                MinWidth = 280
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = combo,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && combo.SelectedItem is PickerOption<T> chosen)
                return chosen.Value;
            return default;
        });

    public Task<string?> ShowInputAsync(string title, string? initialValue = null, string? placeholder = null) =>
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var textBox = new TextBox
            {
                Text = initialValue ?? string.Empty,
                Watermark = placeholder,
                MinWidth = 320
            };

            var dialog = new ContentDialog
            {
                Title = title,
                Content = textBox,
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary ? textBox.Text : null;
        });

    private sealed record PickerOption<T>(T Value, string Label)
    {
        public override string ToString() => Label;
    }
}
