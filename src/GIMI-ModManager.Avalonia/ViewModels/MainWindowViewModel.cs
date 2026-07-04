using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using GIMI_ModManager.Avalonia.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IRecipient<StartupCompletedMessage>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AppInitializer _appInitializer;
    private readonly ILogger _logger;

    [ObservableProperty] private object? _content;
    [ObservableProperty] private bool _isLoading = true;

    public MainWindowViewModel(IServiceProvider serviceProvider, AppInitializer appInitializer,
        IMessenger messenger, ILogger logger)
    {
        _serviceProvider = serviceProvider;
        _appInitializer = appInitializer;
        _logger = logger.ForContext<MainWindowViewModel>();
        messenger.RegisterAll(this);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _appInitializer.PrepareSelectedGameAsync();
            var game = await _appInitializer.GetSelectedGameAsync();

            if (await _appInitializer.IsConfiguredAsync(game))
            {
                await _appInitializer.InitializeForCurrentGameAsync();
                ShowShell();
            }
            else
            {
                _logger.Information("JASM not configured for {Game}; showing first-time setup.", game);
                var startup = _serviceProvider.GetRequiredService<StartupViewModel>();
                await startup.InitializeAsync();
                Content = startup;
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Startup initialization failed; falling back to first-time setup.");
            var startup = _serviceProvider.GetRequiredService<StartupViewModel>();
            await startup.InitializeAsync();
            Content = startup;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ShowShell()
    {
        var shell = _serviceProvider.GetRequiredService<ShellViewModel>();
        Content = shell;
        _ = shell.InitializeAsync();
    }

    public void Receive(StartupCompletedMessage message) => ShowShell();
}
