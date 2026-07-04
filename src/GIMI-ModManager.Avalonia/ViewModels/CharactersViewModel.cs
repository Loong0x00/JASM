using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Avalonia.ViewModels.Items;
using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.GamesService;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

public partial class CharactersViewModel : ViewModelBase, INavigationAware
{
    private readonly IGameService _gameService;
    private readonly ISkinManagerService _skinManagerService;
    private readonly INavigationService _navigation;
    private readonly ILogger _logger;

    private readonly List<CharacterGridItem> _allItems = new();

    public ObservableCollection<CharacterGridItem> Characters { get; } = new();

    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private bool _onlyWithMods;
    [ObservableProperty] private int _totalMods;
    [ObservableProperty] private bool _isLoading;

    public CharactersViewModel(IGameService gameService, ISkinManagerService skinManagerService,
        INavigationService navigation, ILogger logger)
    {
        _gameService = gameService;
        _skinManagerService = skinManagerService;
        _navigation = navigation;
        _logger = logger.ForContext<CharactersViewModel>();
    }

    public Task OnNavigatedToAsync(object? parameter) => LoadAsync();

    private Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allItems.Clear();
            var totalMods = 0;

            foreach (var obj in _gameService.GetAllModdableObjects(GetOnly.Enabled))
            {
                var item = new CharacterGridItem(obj);
                var modList = _skinManagerService.GetCharacterModListOrDefault(obj.InternalName);
                if (modList is not null)
                {
                    var mods = modList.Mods;
                    item.ModCount = mods.Count;
                    item.EnabledModCount = mods.Count(m => m.IsEnabled);
                    totalMods += mods.Count;
                }

                _allItems.Add(item);
            }

            TotalMods = totalMods;
            ApplyFilter();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Failed to load characters");
        }
        finally
        {
            IsLoading = false;
        }

        return Task.CompletedTask;
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnOnlyWithModsChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<CharacterGridItem> query = _allItems;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(i =>
                i.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                i.InternalName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (OnlyWithMods)
            query = query.Where(i => i.HasMods);

        // Characters with mods first, then by name.
        var ordered = query
            .OrderByDescending(i => i.HasMods)
            .ThenByDescending(i => i.ModCount)
            .ThenBy(i => i.DisplayName, StringComparer.CurrentCulture)
            .ToList();

        Characters.Clear();
        foreach (var item in ordered)
            Characters.Add(item);
    }

    [RelayCommand]
    private Task OpenCharacter(CharacterGridItem? item)
    {
        if (item is null)
            return Task.CompletedTask;
        return _navigation.NavigateToAsync<CharacterDetailsViewModel>(item.ModdableObject);
    }
}
