using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using GIMI_ModManager.Avalonia.Services;
using GIMI_ModManager.Avalonia.Services.Dialogs;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Avalonia.Services.Settings;
using GIMI_ModManager.Avalonia.ViewModels;
using GIMI_ModManager.Avalonia.Views;
using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GIMI_ModManager.Avalonia;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        Services = ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = mainViewModel };

            _ = BootstrapAsync(mainViewModel);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static async Task BootstrapAsync(MainWindowViewModel mainViewModel)
    {
        try
        {
            var settings = Services.GetRequiredService<ILocalSettingsService>();
            var theme = await settings.ReadSettingAsync<string>("AppTheme", SettingScope.App);
            await Dispatcher.UIThread.InvokeAsync(() => ApplyTheme(theme));

            // Apply the saved UI language before the game data loads, so the localizer picks the
            // right Languages/<code> asset overrides for character names.
            var lang = await settings.ReadSettingAsync<string>(ViewModels.SettingsViewModel.LanguageSettingKey,
                SettingScope.App);
            if (!string.IsNullOrWhiteSpace(lang))
                await Services.GetRequiredService<ILanguageLocalizer>().SetLanguageAsync(lang);
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to apply saved theme/language");
        }

        await mainViewModel.InitializeAsync();
    }

    private static void ApplyTheme(string? theme)
    {
        if (Current is null)
            return;

        Current.RequestedThemeVariant = theme switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private static IServiceProvider ConfigureServices()
    {
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(logsDir, "log.txt"),
                rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        var services = new ServiceCollection();

        services.AddSingleton<ILogger>(Log.Logger);
        services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

        // Core services (cross-platform logic layer, reused as-is)
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<ILanguageLocalizer>(_ => new SimpleLocalizer("zh-cn"));
        services.AddSingleton<IGameService, GameService>();
        services.AddSingleton<ModCrawlerService>();
        services.AddSingleton<ISkinManagerService, SkinManagerService>();
        services.AddSingleton<ArchiveService>();
        services.AddSingleton<ModInstallService>();
        services.AddSingleton<GIMI_ModManager.Core.Services.ModPresetService.ModPresetService>();

        // App/UI services (Avalonia replacements for the WinUI shell services)
        services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
        services.AddSingleton<SelectedGameService>();
        services.AddSingleton<AppInitializer>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // ViewModels
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<StartupViewModel>();
        services.AddTransient<CharactersViewModel>();
        services.AddTransient<CharacterDetailsViewModel>();
        services.AddTransient<ModPaneViewModel>();
        services.AddTransient<PresetsViewModel>();
        services.AddTransient<ModsOverviewViewModel>();
        services.AddTransient<SettingsViewModel>();

        return services.BuildServiceProvider();
    }
}
