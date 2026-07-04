using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Services;
using GIMI_ModManager.Avalonia.Services.Dialogs;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Avalonia.ViewModels.Items;
using GIMI_ModManager.Core.Contracts.Entities;
using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.Entities.Mods.SkinMod;
using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.GamesService.Interfaces;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

public partial class CharacterDetailsViewModel : ViewModelBase, INavigationAware
{
    private readonly ISkinManagerService _skinManagerService;
    private readonly IGameService _gameService;
    private readonly INavigationService _navigation;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly IFilePickerService _filePicker;
    private readonly ModInstallService _modInstaller;
    private readonly ILogger _logger;

    private IModdableObject? _character;
    private ICharacterModList? _modList;
    private readonly List<ModRowItem> _allMods = new();

    public ObservableCollection<ModRowItem> Mods { get; } = new();
    public ModPaneViewModel Pane { get; }

    [ObservableProperty] private string _characterName = string.Empty;
    [ObservableProperty] private string? _characterImagePath;
    [ObservableProperty] private string? _modSearchText;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private int _selectedCount;
    [ObservableProperty] private ModRowItem? _activeMod;
    [ObservableProperty] private bool _canHideCharacter;
    [ObservableProperty] private bool _isCharacterHidden;

    public bool HasSelection => SelectedCount > 0;
    public string HideCharacterLabel => IsCharacterHidden ? "取消隐藏角色" : "隐藏角色";

    partial void OnIsCharacterHiddenChanged(bool value) => OnPropertyChanged(nameof(HideCharacterLabel));

    public CharacterDetailsViewModel(ISkinManagerService skinManagerService, IGameService gameService,
        INavigationService navigation, IDialogService dialogs, INotificationService notifications,
        IFilePickerService filePicker, ModInstallService modInstaller, ModPaneViewModel pane, ILogger logger)
    {
        _skinManagerService = skinManagerService;
        _gameService = gameService;
        _navigation = navigation;
        _dialogs = dialogs;
        _notifications = notifications;
        _filePicker = filePicker;
        _modInstaller = modInstaller;
        Pane = pane;
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

        CanHideCharacter = character is ICharacter;
        IsCharacterHidden = character is ICharacter &&
                            _gameService.GetDisabledCharacters()
                                .Any(c => c.InternalNameEquals(character.InternalName));

        await LoadModsAsync();
    }

    [RelayCommand]
    private async Task ToggleHideCharacter()
    {
        if (_character is not ICharacter character)
            return;

        try
        {
            if (IsCharacterHidden)
                await _gameService.EnableCharacterAsync(character);
            else
                await _gameService.DisableCharacterAsync(character);

            IsCharacterHidden = !IsCharacterHidden;
            _notifications.ShowInfo(IsCharacterHidden ? "已隐藏角色" : "已取消隐藏",
                $"{CharacterName} {(IsCharacterHidden ? "已从角色列表隐藏" : "已恢复显示")}。");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to toggle character hidden state");
            _notifications.ShowError("操作失败", e.Message);
        }
    }

    private async Task LoadModsAsync()
    {
        if (_modList is null)
            return;

        IsLoading = true;
        try
        {
            foreach (var row in _allMods)
                row.PropertyChanged -= OnRowPropertyChanged;

            _allMods.Clear();
            Pane.Clear();
            ActiveMod = null;

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
                row.PropertyChanged += OnRowPropertyChanged;
                _allMods.Add(row);
            }

            ApplyFilter();
            IsEmpty = _allMods.Count == 0;
            RecomputeSelection();

            // Auto-select the first mod so the detail pane is populated on arrival.
            ActiveMod = Mods.FirstOrDefault();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnRowPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ModRowItem.IsSelected))
            RecomputeSelection();
    }

    private void RecomputeSelection()
    {
        SelectedCount = _allMods.Count(m => m.IsSelected);
        OnPropertyChanged(nameof(HasSelection));
    }

    partial void OnModSearchTextChanged(string? value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<ModRowItem> query = _allMods;
        if (!string.IsNullOrWhiteSpace(ModSearchText))
        {
            var term = ModSearchText.Trim();
            query = query.Where(m =>
                m.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                m.FolderName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (m.Author?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Mods.Clear();
        foreach (var m in query)
            Mods.Add(m);
    }

    partial void OnActiveModChanged(ModRowItem? value)
    {
        if (value is null || _modList is null)
        {
            Pane.Clear();
            return;
        }

        var entry = _modList.Mods.FirstOrDefault(e => e.Id == value.Id);
        if (entry is not null)
            _ = Pane.LoadAsync(entry.Mod);
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

    private IReadOnlyList<ModRowItem> SelectedOrActive()
    {
        var selected = _allMods.Where(m => m.IsSelected).ToList();
        if (selected.Count > 0)
            return selected;
        return ActiveMod is not null ? new List<ModRowItem> { ActiveMod } : Array.Empty<ModRowItem>();
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
            var entry = _modList.Mods.FirstOrDefault(e => e.Id == row.Id);
            if (entry is not null)
            {
                row.SetEnabled(entry.IsEnabled);
                row.DisplayName = entry.Mod.GetDisplayName();
            }
        }
    }

    private void SetModEnabledState(bool enable)
    {
        if (_modList is null)
            return;

        var targets = SelectedOrActive();
        if (targets.Count == 0)
            return;

        try
        {
            using (_modList.DisableWatcher())
            {
                foreach (var row in targets)
                {
                    try
                    {
                        if (enable)
                            _modList.EnableMod(row.Id);
                        else
                            _modList.DisableMod(row.Id);
                    }
                    catch (InvalidOperationException)
                    {
                        // Already in the desired state; ignore.
                    }
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Batch enable/disable failed");
            _notifications.ShowError("批量操作失败", e.Message);
        }
        finally
        {
            foreach (var row in targets)
            {
                var entry = _modList.Mods.FirstOrDefault(e => e.Id == row.Id);
                if (entry is not null)
                {
                    row.SetEnabled(entry.IsEnabled);
                    row.DisplayName = entry.Mod.GetDisplayName();
                }
            }
        }
    }

    [RelayCommand]
    private void EnableSelected() => SetModEnabledState(true);

    [RelayCommand]
    private void DisableSelected() => SetModEnabledState(false);

    [RelayCommand]
    private async Task DeleteSelected()
    {
        if (_modList is null)
            return;

        var targets = SelectedOrActive();
        if (targets.Count == 0)
            return;

        var confirmed = await _dialogs.ShowConfirmAsync("删除模组",
            $"确定要删除选中的 {targets.Count} 个模组吗？\n它们会被移动到系统回收站/垃圾桶。",
            "删除", "取消");
        if (!confirmed)
            return;

        try
        {
            using (_modList.DisableWatcher())
            {
                foreach (var row in targets)
                    _modList.DeleteModBySkinEntryId(row.Id, moveToRecycleBin: true);
            }

            _notifications.ShowSuccess("已删除", $"已删除 {targets.Count} 个模组。");
            await LoadModsAsync();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to delete mods");
            _notifications.ShowError("删除失败", e.Message);
        }
    }

    [RelayCommand]
    private async Task MoveSelected()
    {
        if (_modList is null || _character is null)
            return;

        var targets = SelectedOrActive();
        if (targets.Count == 0)
            return;

        var candidates = _gameService.GetAllModdableObjects(GetOnly.Enabled)
            .Where(o => !o.InternalNameEquals(_character.InternalName))
            .OrderBy(o => o.DisplayName, StringComparer.CurrentCulture)
            .ToList();

        var target = await _dialogs.ShowPickerAsync("移动模组到…", candidates, o => o.DisplayName, "选择目标角色");
        if (target is null)
            return;

        try
        {
            var targetList = _skinManagerService.GetCharacterModList(target);
            using (_modList.DisableWatcher())
            using (targetList.DisableWatcher())
            {
                foreach (var row in targets)
                {
                    var entry = _modList.Mods.FirstOrDefault(e => e.Id == row.Id);
                    if (entry is not null)
                        _skinManagerService.AddMod(entry.Mod, targetList, move: true);
                }
            }

            await _skinManagerService.RefreshModsAsync(_character.InternalName);
            _notifications.ShowSuccess("已移动", $"已移动 {targets.Count} 个模组到 {target.DisplayName}。");
            await LoadModsAsync();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to move mods");
            _notifications.ShowError("移动失败", e.Message);
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var row in Mods)
            row.IsSelected = true;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var row in _allMods)
            row.IsSelected = false;
    }

    [RelayCommand]
    private void OpenModFolder(ModRowItem? row)
    {
        if (row is not null)
            PlatformService.OpenInFileManager(row.FolderPath);
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

            row.PropertyChanged -= OnRowPropertyChanged;
            _allMods.Remove(row);
            Mods.Remove(row);
            IsEmpty = _allMods.Count == 0;
            if (ReferenceEquals(ActiveMod, row))
                ActiveMod = null;
            RecomputeSelection();
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
    private async Task InstallArchive()
    {
        if (_modList is null)
            return;

        var filters = new[]
        {
            new global::Avalonia.Platform.Storage.FilePickerFileType("模组压缩包")
            {
                Patterns = new[] { "*.zip", "*.rar", "*.7z" }
            }
        };
        var archive = await _filePicker.PickFileAsync("选择模组压缩包", filters);
        if (archive is null)
            return;

        try
        {
            var modRoot = _modInstaller.ExtractArchive(archive);
            await _modInstaller.InstallAsync(modRoot.FullName, _modList, move: true);
            await LoadModsAsync();
            _notifications.ShowSuccess("已安装", $"模组已从压缩包安装到 {CharacterName}。");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to install archive {Archive}", archive);
            _notifications.ShowError("安装失败", e.Message);
        }
        finally
        {
            ModInstallService.CleanupTemp();
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
