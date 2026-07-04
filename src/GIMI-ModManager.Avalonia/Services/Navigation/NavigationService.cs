using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GIMI_ModManager.Avalonia.Services.Navigation;

public partial class NavigationService : ObservableObject, INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly Stack<object> _backStack = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    private object? _currentPage;

    public bool CanGoBack => _backStack.Count > 0;

    public event EventHandler? Navigated;

    public NavigationService(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger.ForContext<NavigationService>();
    }

    public Task NavigateToAsync<TViewModel>(object? parameter = null, bool clearHistory = false) =>
        NavigateToAsync(typeof(TViewModel), parameter, clearHistory);

    public async Task NavigateToAsync(Type viewModelType, object? parameter = null, bool clearHistory = false)
    {
        var target = _serviceProvider.GetRequiredService(viewModelType);

        if (clearHistory)
            _backStack.Clear();
        else if (CurrentPage is not null && !ReferenceEquals(CurrentPage, target))
            _backStack.Push(CurrentPage);

        CurrentPage = target;
        OnPropertyChanged(nameof(CanGoBack));
        Navigated?.Invoke(this, EventArgs.Empty);

        if (target is INavigationAware aware)
        {
            try
            {
                await aware.OnNavigatedToAsync(parameter);
            }
            catch (Exception e)
            {
                _logger.Error(e, "OnNavigatedToAsync failed for {ViewModel}", viewModelType.Name);
            }
        }
    }

    public async Task GoBackAsync()
    {
        if (_backStack.Count == 0)
            return;

        var previous = _backStack.Pop();
        CurrentPage = previous;
        OnPropertyChanged(nameof(CanGoBack));
        Navigated?.Invoke(this, EventArgs.Empty);

        if (previous is INavigationAware aware)
        {
            try
            {
                await aware.OnNavigatedToAsync(null);
            }
            catch (Exception e)
            {
                _logger.Error(e, "OnNavigatedToAsync (back) failed for {ViewModel}", previous.GetType().Name);
            }
        }
    }
}
