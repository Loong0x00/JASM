using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Services;
using GIMI_ModManager.Avalonia.Services.Dialogs;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Avalonia.ViewModels.Items;
using GIMI_ModManager.Core.Contracts.Entities;
using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.Entities.Mods.SkinMod;
using GIMI_ModManager.Core.GamesService.Interfaces;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

public partial class CharacterDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly ISkinManagerService _skinManagerService;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly IFilePickerService _filePicker;
    private readonly ILogger _logger;

    private IModdableObject? _character;
    private ICharacterModList? _modList;

    public ObservableCollection<ModRowItem> Mods { get; } = new();

    [ObservableProperty] private string _characterName = string.Empty;
    [ObservableProperty] private string? _characterImagePath;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty;

    public CharacterDetailsViewModel(ISkinManagerService skinManagerService, INavigationService navigation,
        IDialogService dialogs, INotificationService notifications, IFilePickerService filePicker, ILogger logger)
    {
        _skinManagerService = skinManagerService;
        _navigation = navigation;
        _dialogs = dialogs;
        _notifications = notifications;
        _filePicker = filePicker;
        _logger = logger.ForContext<CharacterDetailsViewModel>();
    }

    public async Task OnNavigatedToAsync(object? parameter)
    {
        if (parameter is not IModdableObject character)
        {
            _logger.Warning("CharacterDetails navigated without a moddable object");
            return;
        }

        _character = character;
        CharacterName = character.DisplayName;
        CharacterImagePath = character.ImageUri?.LocalPath;
        _modList = _skinManagerService.GetCharacterModList(character);
        await LoadModsAsync();
    }

    private async Task LoadModsAsync()
    {
        if (_modList is null)
            return;

        IsLoading = true;
        try
        {
            Mods.Clear();
            foreach (var entry in _modList.Mods.OrderBy(e => e.Mod.GetDisplayName(), StringComparer.CurrentCulture))
            {
                var settings = await SafeReadSettings(entry.Mod);
                var row = new ModRowItem(
                    entry.Id,
                    entry.Mod.GetDisplayName(),
                    entry.IsEnabled,
                    entry.Mod.Name,
                    entry.Mod.FullPath)
                {
                    Author = settings?.Author,
                    ModUrl = settings?.ModUrl?.ToString(),
                    DateAdded = settings?.DateAdded
                };
                Mods.Add(row);
            }

            IsEmpty = Mods.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task<GIMI_ModManager.Core.Entities.Mods.Contract.ModSettings?> SafeReadSettings(ISkinMod mod)
    {
        try
        {
            return await mod.Settings.TryReadSettingsAsync(useCache: true);
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed to read settings for mod {Mod}", mod.Name);
            return null;
        }
    }

    [RelayCommand]
    private async Task ToggleMod(ModRowItem? row)
    {
        if (row is null || _modList is null)
            return;

        try
        {
            using (_modList.DisableWatcher())
            {
                _modList.ToggleMod(row.Id);
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to toggle mod {Mod}", row.FolderName);
            _notifications.ShowError("切换失败", e.Message);
        }
        finally
        {
            // Re-sync the row from the authoritative mod list, whether the toggle succeeded or not
            // (the folder name also changes as the DISABLED_ prefix is added/removed).
            var entry = _modList.Mods.FirstOrDefault(e => e.Id == row.Id);
            if (entry is not null)
            {
                row.SetEnabled(entry.IsEnabled);
                row.DisplayName = entry.Mod.GetDisplayName();
            }
        }
    }

    [RelayCommand]
    private void OpenModFolder(ModRowItem? row)
    {
        if (row is not null)
            PlatformService.OpenInFileManager(row.FolderPath);
    }

    [RelayCommand]
    private void OpenModUrl(ModRowItem? row)
    {
        if (!string.IsNullOrWhiteSpace(row?.ModUrl))
            PlatformService.OpenUrl(row.ModUrl);
    }

    [RelayCommand]
    private async Task DeleteMod(ModRowItem? row)
    {
        if (row is null || _modList is null)
            return;

        var confirmed = await _dialogs.ShowConfirmAsync("删除模组",
            $"确定要删除模组 “{row.DisplayName}” 吗？\n它会被移动到系统回收站/垃圾桶。",
            "删除", "取消");
        if (!confirmed)
            return;

        try
        {
            using (_modList.DisableWatcher())
            {
                _modList.DeleteModBySkinEntryId(row.Id, moveToRecycleBin: true);
            }

            Mods.Remove(row);
            IsEmpty = Mods.Count == 0;
            _notifications.ShowSuccess("已删除", $"模组 “{row.DisplayName}” 已移入回收站。");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to delete mod {Mod}", row.FolderName);
            _notifications.ShowError("删除失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task AddMod()
    {
        if (_modList is null || _character is null)
            return;

        var folder = await _filePicker.PickFolderAsync("选择要添加的模组文件夹");
        if (folder is null)
            return;

        try
        {
            var mod = await SkinMod.CreateModAsync(folder);
            using (_modList.DisableWatcher())
            {
                _skinManagerService.AddMod(mod, _modList, move: false);
            }

            await LoadModsAsync();
            _notifications.ShowSuccess("已添加", $"模组已添加到 {CharacterName}。");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to add mod from {Folder}", folder);
            _notifications.ShowError("添加失败", e.Message);
        }
    }

    [RelayCommand]
    private void OpenCharacterFolder()
    {
        if (_modList is not null)
            PlatformService.OpenInFileManager(_modList.AbsModsFolderPath);
    }

    [RelayCommand]
    private async Task Refresh()
    {
        if (_character is null)
            return;
        await _skinManagerService.RefreshModsAsync(_character.InternalName);
        await LoadModsAsync();
    }

    [RelayCommand]
    private Task GoBack() => _navigation.GoBackAsync();
}
