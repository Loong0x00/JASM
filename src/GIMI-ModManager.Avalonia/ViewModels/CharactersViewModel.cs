using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Services;
using GIMI_ModManager.Avalonia.Services.Dialogs;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Avalonia.Services.Settings;
using GIMI_ModManager.Avalonia.ViewModels.Items;
using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.GamesService.Interfaces;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

public partial class CharactersViewModel : ViewModelBase, INavigationAware
{
    private const string All = "全部";
    private const string PinnedKey = "PinnedCharacters";

    private readonly IGameService _gameService;
    private readonly ISkinManagerService _skinManagerService;
    private readonly INavigationService _navigation;
    private readonly ILocalSettingsService _localSettings;
    private readonly ModInstallService _modInstaller;
    private readonly IFilePickerService _filePicker;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly ILogger _logger;

    private readonly List<CharacterGridItem> _allItems = new();
    private readonly HashSet<string> _pinned = new(StringComparer.OrdinalIgnoreCase);
    private bool _loaded;
    private bool _suppressFilter;

    public ObservableCollection<CharacterGridItem> Characters { get; } = new();
    public ObservableCollection<ICategory> Categories { get; } = new();
    public ObservableCollection<string> Elements { get; } = new();
    public ObservableCollection<string> Classes { get; } = new();

    public IReadOnlyList<string> SortOptions { get; } =
        new[] { "模组数量", "名称", "稀有度", "发布日期" };

    [ObservableProperty] private ICategory? _selectedCategory;
    [ObservableProperty] private string _selectedElement = All;
    [ObservableProperty] private string _selectedClass = All;
    [ObservableProperty] private string _selectedSort = "模组数量";
    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private bool _onlyWithMods;
    [ObservableProperty] private bool _showHidden;
    [ObservableProperty] private bool _isCharacterCategory;
    [ObservableProperty] private int _totalMods;
    [ObservableProperty] private bool _isLoading;

    public CharactersViewModel(IGameService gameService, ISkinManagerService skinManagerService,
        INavigationService navigation, ILocalSettingsService localSettings, ModInstallService modInstaller,
        IFilePickerService filePicker, IDialogService dialogs, INotificationService notifications, ILogger logger)
    {
        _gameService = gameService;
        _skinManagerService = skinManagerService;
        _navigation = navigation;
        _localSettings = localSettings;
        _modInstaller = modInstaller;
        _filePicker = filePicker;
        _dialogs = dialogs;
        _notifications = notifications;
        _logger = logger.ForContext<CharactersViewModel>();
    }

    public async Task OnNavigatedToAsync(object? parameter)
    {
        if (!_loaded)
        {
            var pinned = await _localSettings.ReadSettingAsync<string[]>(PinnedKey) ?? Array.Empty<string>();
            foreach (var p in pinned)
                _pinned.Add(p);

            foreach (var category in _gameService.GetCategories())
                Categories.Add(category);

            _loaded = true;

            // Selecting the category triggers the initial load.
            SelectedCategory = Categories.FirstOrDefault(c => c.ModCategory == ModCategory.Character)
                               ?? Categories.FirstOrDefault();
        }
        else
        {
            // Returning to the page: refresh mod counts (they may have changed).
            LoadCategoryItems();
        }
    }

    partial void OnSelectedCategoryChanged(ICategory? value) => LoadCategoryItems();
    partial void OnShowHiddenChanged(bool value) => LoadCategoryItems();

    private void LoadCategoryItems()
    {
        if (SelectedCategory is null)
            return;

        IsLoading = true;
        try
        {
            _allItems.Clear();
            IsCharacterCategory = SelectedCategory.ModCategory == ModCategory.Character;

            var getOnly = IsCharacterCategory && ShowHidden ? GetOnly.Both : GetOnly.Enabled;
            var hiddenSet = new HashSet<string>(
                _gameService.GetDisabledCharacters().Select(c => (string)c.InternalName),
                StringComparer.OrdinalIgnoreCase);

            var total = 0;
            foreach (var obj in _gameService.GetModdableObjects(SelectedCategory, getOnly))
            {
                var item = new CharacterGridItem(obj)
                {
                    IsPinned = _pinned.Contains(obj.InternalName),
                    IsHidden = hiddenSet.Contains(obj.InternalName)
                };
                var modList = _skinManagerService.GetCharacterModListOrDefault(obj.InternalName);
                if (modList is not null)
                {
                    var mods = modList.Mods;
                    item.ModCount = mods.Count;
                    item.EnabledModCount = mods.Count(m => m.IsEnabled);
                    total += mods.Count;
                }

                _allItems.Add(item);
            }

            TotalMods = total;
            RebuildFilterOptions();
            ApplyFilter();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to load category {Category}", SelectedCategory?.InternalName);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void RebuildFilterOptions()
    {
        _suppressFilter = true;

        Elements.Clear();
        Elements.Add(All);
        foreach (var e in _allItems.Select(i => i.ElementName)
                     .Where(e => !string.IsNullOrEmpty(e)).Distinct().OrderBy(e => e))
            Elements.Add(e!);

        Classes.Clear();
        Classes.Add(All);
        foreach (var c in _allItems.Select(i => i.ClassName)
                     .Where(c => !string.IsNullOrEmpty(c)).Distinct().OrderBy(c => c))
            Classes.Add(c!);

        SelectedElement = All;
        SelectedClass = All;
        _suppressFilter = false;
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnOnlyWithModsChanged(bool value) => ApplyFilter();
    partial void OnSelectedElementChanged(string value) => ApplyFilter();
    partial void OnSelectedClassChanged(string value) => ApplyFilter();
    partial void OnSelectedSortChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (_suppressFilter)
            return;

        IEnumerable<CharacterGridItem> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(i =>
                i.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.InternalName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (IsCharacterCategory)
        {
            if (SelectedElement != All)
                query = query.Where(i => i.ElementName == SelectedElement);
            if (SelectedClass != All)
                query = query.Where(i => i.ClassName == SelectedClass);
        }

        if (OnlyWithMods)
            query = query.Where(i => i.HasMods);

        Func<CharacterGridItem, IComparable> keySelector = SelectedSort switch
        {
            "名称" => i => i.DisplayName,
            "稀有度" => i => -i.Rarity,
            "发布日期" => i => -(i.ReleaseDate ?? DateTime.MinValue).Ticks,
            _ => i => -i.ModCount
        };

        // Pinned always float to the top, then by the chosen sort, then by name.
        var ordered = query
            .OrderByDescending(i => i.IsPinned)
            .ThenBy(keySelector)
            .ThenBy(i => i.DisplayName, StringComparer.CurrentCulture)
            .ToList();

        Characters.Clear();
        foreach (var item in ordered)
            Characters.Add(item);
    }

    [RelayCommand]
    private async Task TogglePin(CharacterGridItem? item)
    {
        if (item is null)
            return;

        item.IsPinned = !item.IsPinned;
        if (item.IsPinned)
            _pinned.Add(item.InternalName);
        else
            _pinned.Remove(item.InternalName);

        ApplyFilter();

        try
        {
            await _localSettings.SaveSettingAsync(PinnedKey, _pinned.ToArray());
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed to persist pinned characters");
        }
    }

    [RelayCommand]
    private Task OpenCharacter(CharacterGridItem? item)
    {
        if (item is null)
            return Task.CompletedTask;
        return _navigation.NavigateToAsync<CharacterDetailsViewModel>(item.ModdableObject);
    }

    [RelayCommand]
    private async Task InstallMod()
    {
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

        var candidates = _gameService.GetAllModdableObjects(GetOnly.Enabled)
            .OrderBy(o => o.DisplayName, StringComparer.CurrentCulture)
            .ToList();

        var target = await _dialogs.ShowPickerAsync("安装到哪个角色？", candidates, o => o.DisplayName, "选择角色");
        if (target is null)
            return;

        try
        {
            var modList = _skinManagerService.GetCharacterModList(target);
            var modRoot = _modInstaller.ExtractArchive(archive);
            await _modInstaller.InstallAsync(modRoot.FullName, modList, move: true);
            _notifications.ShowSuccess("已安装", $"模组已安装到 {target.DisplayName}。");
            LoadCategoryItems();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to install mod from {Archive}", archive);
            _notifications.ShowError("安装失败", e.Message);
        }
        finally
        {
            ModInstallService.CleanupTemp();
        }
    }
}
