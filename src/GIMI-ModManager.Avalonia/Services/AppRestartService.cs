using System.Diagnostics;
using System.Reflection;
using global::Avalonia;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Threading;

namespace GIMI_ModManager.Avalonia.Services;

/// <summary>
/// Relaunches the application. Used when switching games, since the Core GameService can only be
/// initialized once per process — a clean restart re-runs the boot path for the newly selected game.
/// </summary>
public static class AppRestartService
{
    public static void Restart()
    {
        var exe = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exe))
        {
            ProcessStartInfo psi;
            var dll = Assembly.GetEntryAssembly()?.Location;

            // When launched via `dotnet App.dll` the process is the shared dotnet host; pass the dll back.
            if (Path.GetFileNameWithoutExtension(exe).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(dll))
            {
                psi = new ProcessStartInfo(exe, $"\"{dll}\"");
            }
            else
            {
                psi = new ProcessStartInfo(exe);
            }

            psi.UseShellExecute = false;
            psi.WorkingDirectory = AppContext.BaseDirectory;

            try
            {
                Process.Start(psi);
            }
            catch
            {
                // If relaunch fails, fall through to shutdown; the user can start it again.
            }
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.Shutdown();
            else
                Environment.Exit(0);
        });
    }
}
