using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Core.GamesService;

namespace GIMI_ModManager.Avalonia.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly IGameService _gameService;

    public INavigationService Navigation { get; }

    [ObservableProperty] private string _gameName = "JASM";

    public ShellViewModel(INavigationService navigation, IGameService gameService)
    {
        Navigation = navigation;
        _gameService = gameService;
    }

    public async Task InitializeAsync()
    {
        GameName = _gameService.GameName;
        await Navigation.NavigateToAsync<CharactersViewModel>(clearHistory: true);
    }

    [RelayCommand]
    private Task NavigateCharacters() => Navigation.NavigateToAsync<CharactersViewModel>();

    [RelayCommand]
    private Task NavigatePresets() => Navigation.NavigateToAsync<PresetsViewModel>();

    [RelayCommand]
    private Task NavigateOverview() => Navigation.NavigateToAsync<ModsOverviewViewModel>();

    [RelayCommand]
    private Task NavigateSettings() => Navigation.NavigateToAsync<SettingsViewModel>();

    [RelayCommand]
    private Task GoBack() => Navigation.GoBackAsync();
}
