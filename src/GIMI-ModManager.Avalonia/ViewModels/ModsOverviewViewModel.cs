using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Avalonia.ViewModels.Items;
using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.GamesService;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

/// <summary>
/// A flat list of every mod across all characters (the original ModsOverviewPage), with search
/// and a jump-to-character action.
/// </summary>
public partial class ModsOverviewViewModel : ViewModelBase, INavigationAware
{
    private readonly IGameService _gameService;
    private readonly ISkinManagerService _skinManagerService;
    private readonly INavigationService _navigation;
    private readonly ILogger _logger;

    private readonly List<OverviewRow> _allRows = new();

    public ObservableCollection<OverviewRow> Rows { get; } = new();

    [ObservableProperty] private string? _searchText;
    [ObservableProperty] private bool _onlyEnabled;
    [ObservableProperty] private int _totalMods;
    [ObservableProperty] private int _enabledMods;

    public ModsOverviewViewModel(IGameService gameService, ISkinManagerService skinManagerService,
        INavigationService navigation, ILogger logger)
    {
        _gameService = gameService;
        _skinManagerService = skinManagerService;
        _navigation = navigation;
        _logger = logger.ForContext<ModsOverviewViewModel>();
    }

    public Task OnNavigatedToAsync(object? parameter)
    {
        Load();
        return Task.CompletedTask;
    }

    private void Load()
    {
        _allRows.Clear();
        var enabled = 0;

        foreach (var obj in _gameService.GetAllModdableObjects(GetOnly.Enabled))
        {
            var modList = _skinManagerService.GetCharacterModListOrDefault(obj.InternalName);
            if (modList is null)
                continue;

            foreach (var entry in modList.Mods)
            {
                if (entry.IsEnabled)
                    enabled++;
                _allRows.Add(new OverviewRow(obj, entry.Mod.GetDisplayName(), entry.IsEnabled, entry.Mod.FullPath));
            }
        }

        TotalMods = _allRows.Count;
        EnabledMods = enabled;
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string? value) => ApplyFilter();
    partial void OnOnlyEnabledChanged(bool value) => ApplyFilter();

    private void ApplyFilter()
    {
        IEnumerable<OverviewRow> query = _allRows;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query = query.Where(r =>
                r.ModName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                r.CharacterName.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (OnlyEnabled)
            query = query.Where(r => r.IsEnabled);

        var ordered = query
            .OrderBy(r => r.CharacterName, StringComparer.CurrentCulture)
            .ThenBy(r => r.ModName, StringComparer.CurrentCulture)
            .ToList();

        Rows.Clear();
        foreach (var row in ordered)
            Rows.Add(row);
    }

    [RelayCommand]
    private Task OpenCharacter(OverviewRow? row)
    {
        if (row is null)
            return Task.CompletedTask;
        return _navigation.NavigateToAsync<CharacterDetailsViewModel>(row.Character);
    }
}
