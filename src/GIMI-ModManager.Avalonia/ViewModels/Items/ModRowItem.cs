using CommunityToolkit.Mvvm.ComponentModel;

namespace GIMI_ModManager.Avalonia.ViewModels.Items;

public partial class ModRowItem : ObservableObject
{
    public Guid Id { get; }
    public string FolderName { get; }
    public string FolderPath { get; }

    [ObservableProperty] private string _displayName;
    [ObservableProperty] private bool _isEnabled;
    [ObservableProperty] private string? _author;
    [ObservableProperty] private string? _modUrl;
    [ObservableProperty] private DateTime? _dateAdded;

    public string ToggleLabel => IsEnabled ? "禁用" : "启用";
    public string StatusText => IsEnabled ? "已启用" : "已禁用";

    public ModRowItem(Guid id, string displayName, bool isEnabled, string folderName, string folderPath)
    {
        Id = id;
        _displayName = displayName;
        _isEnabled = isEnabled;
        FolderName = folderName;
        FolderPath = folderPath;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        OnPropertyChanged(nameof(ToggleLabel));
        OnPropertyChanged(nameof(StatusText));
    }

    /// <summary>
    /// Sets the enabled flag to the authoritative value from the mod list and always re-raises
    /// change notification, so a OneWay-bound ToggleSwitch snaps back even after a no-op/failed toggle.
    /// </summary>
    public void SetEnabled(bool value)
    {
        if (IsEnabled != value)
        {
            IsEnabled = value;
            return;
        }

        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(ToggleLabel));
        OnPropertyChanged(nameof(StatusText));
    }
}
