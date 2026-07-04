using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;
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
}
