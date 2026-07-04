using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Controls.Notifications;
using global::Avalonia.Threading;

namespace GIMI_ModManager.Avalonia.Services;

public interface INotificationService
{
    void ShowInfo(string title, string message);
    void ShowSuccess(string title, string message);
    void ShowWarning(string title, string message);
    void ShowError(string title, string message);
}

public class NotificationService : INotificationService
{
    private WindowNotificationManager? _manager;

    private WindowNotificationManager? GetManager()
    {
        if (_manager is not null)
            return _manager;

        var top = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (top is null)
            return null;

        _manager = new WindowNotificationManager(top)
        {
            Position = NotificationPosition.BottomRight,
            MaxItems = 4
        };
        return _manager;
    }

    private void Show(string title, string message, NotificationType type, TimeSpan? expiration = null) =>
        Dispatcher.UIThread.Post(() =>
            GetManager()?.Show(new Notification(title, message, type, expiration ?? TimeSpan.FromSeconds(5))));

    public void ShowInfo(string title, string message) => Show(title, message, NotificationType.Information);
    public void ShowSuccess(string title, string message) => Show(title, message, NotificationType.Success);
    public void ShowWarning(string title, string message) => Show(title, message, NotificationType.Warning);

    public void ShowError(string title, string message) =>
        Show(title, message, NotificationType.Error, TimeSpan.FromSeconds(10));
}
