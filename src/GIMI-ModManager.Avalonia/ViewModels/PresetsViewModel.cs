using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Services;
using GIMI_ModManager.Avalonia.Services.Dialogs;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Avalonia.ViewModels.Items;
using GIMI_ModManager.Core.Services.ModPresetService;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

/// <summary>
/// Mod presets: named snapshots of which mods are enabled. Mirrors the original PresetPage —
/// create (from current active mods), apply, rename, duplicate, delete, toggle read-only.
/// </summary>
public partial class PresetsViewModel : ViewModelBase, INavigationAware
{
    private readonly ModPresetService _presetService;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly ILogger _logger;

    public ObservableCollection<PresetItem> Presets { get; } = new();

    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private bool _isBusy;

    public PresetsViewModel(ModPresetService presetService, IDialogService dialogs,
        INotificationService notifications, ILogger logger)
    {
        _presetService = presetService;
        _dialogs = dialogs;
        _notifications = notifications;
        _logger = logger.ForContext<PresetsViewModel>();
    }

    public Task OnNavigatedToAsync(object? parameter)
    {
        Load();
        return Task.CompletedTask;
    }

    private void Load()
    {
        Presets.Clear();
        foreach (var preset in _presetService.GetPresets().OrderBy(p => p.Index))
            Presets.Add(new PresetItem(preset));
        IsEmpty = Presets.Count == 0;
    }

    [RelayCommand]
    private async Task CreatePreset()
    {
        var name = await _dialogs.ShowInputAsync("新建预设", placeholder: "预设名称");
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            // createEmptyPreset:false snapshots the currently enabled mods.
            await _presetService.CreatePresetAsync(name.Trim(), createEmptyPreset: false);
            Load();
            _notifications.ShowSuccess("已创建预设", $"预设 “{name.Trim()}” 已保存当前启用的模组。");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to create preset");
            _notifications.ShowError("创建失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task ApplyPreset(PresetItem? item)
    {
        if (item is null)
            return;

        IsBusy = true;
        try
        {
            await _presetService.ApplyPresetAsync(item.Name);
            _notifications.ShowSuccess("已应用", $"已应用预设 “{item.Name}”。");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to apply preset {Preset}", item.Name);
            _notifications.ShowError("应用失败", e.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RenamePreset(PresetItem? item)
    {
        if (item is null)
            return;

        var newName = await _dialogs.ShowInputAsync("重命名预设", item.Name);
        if (string.IsNullOrWhiteSpace(newName) || newName.Trim() == item.Name)
            return;

        try
        {
            await _presetService.RenamePresetAsync(item.Name, newName.Trim());
            Load();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to rename preset {Preset}", item.Name);
            _notifications.ShowError("重命名失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task DuplicatePreset(PresetItem? item)
    {
        if (item is null)
            return;

        try
        {
            await _presetService.DuplicatePresetAsync(item.Name);
            Load();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to duplicate preset {Preset}", item.Name);
            _notifications.ShowError("复制失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task DeletePreset(PresetItem? item)
    {
        if (item is null)
            return;

        var confirmed = await _dialogs.ShowConfirmAsync("删除预设",
            $"确定要删除预设 “{item.Name}” 吗？", "删除", "取消");
        if (!confirmed)
            return;

        try
        {
            await _presetService.DeletePresetAsync(item.Name);
            Load();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to delete preset {Preset}", item.Name);
            _notifications.ShowError("删除失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task ToggleReadOnly(PresetItem? item)
    {
        if (item is null)
            return;

        try
        {
            await _presetService.ToggleReadOnlyAsync(item.Name);
            Load();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to toggle read-only for preset {Preset}", item.Name);
            _notifications.ShowError("操作失败", e.Message);
        }
    }
}
