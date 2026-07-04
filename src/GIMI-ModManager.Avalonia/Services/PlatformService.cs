using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GIMI_ModManager.Avalonia.Services;

/// <summary>Cross-platform helpers for opening folders and links in the desktop environment.</summary>
public static class PlatformService
{
    public static void OpenInFileManager(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start(new ProcessStartInfo("open", $"\"{path}\"") { UseShellExecute = false });
            else
                Process.Start(new ProcessStartInfo("xdg-open", $"\"{path}\"") { UseShellExecute = false });
        }
        catch
        {
            // Best-effort; a missing file manager shouldn't crash the app.
        }
    }

    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start(new ProcessStartInfo("open", url) { UseShellExecute = false });
            else
                Process.Start(new ProcessStartInfo("xdg-open", url) { UseShellExecute = false });
        }
        catch
        {
            // ignore
        }
    }
}
