using System.ComponentModel;

namespace GIMI_ModManager.Avalonia.Services.Navigation;

/// <summary>
/// Frame-free navigation abstraction. The shell binds a ContentControl to
/// <see cref="CurrentPage"/>; the ViewLocator resolves the matching view.
/// </summary>
public interface INavigationService : INotifyPropertyChanged
{
    object? CurrentPage { get; }
    bool CanGoBack { get; }

    event EventHandler? Navigated;

    Task NavigateToAsync(Type viewModelType, object? parameter = null, bool clearHistory = false);
    Task NavigateToAsync<TViewModel>(object? parameter = null, bool clearHistory = false);
    Task GoBackAsync();
}

/// <summary>Implemented by ViewModels that need the navigation parameter / to load data on arrival.</summary>
public interface INavigationAware
{
    Task OnNavigatedToAsync(object? parameter);
}
