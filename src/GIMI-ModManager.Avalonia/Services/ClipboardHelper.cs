using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;

namespace GIMI_ModManager.Avalonia.Services;

public static class ClipboardHelper
{
    public static async Task SetTextAsync(string text)
    {
        var top = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (top?.Clipboard is not null)
            await top.Clipboard.SetTextAsync(text);
    }
}
