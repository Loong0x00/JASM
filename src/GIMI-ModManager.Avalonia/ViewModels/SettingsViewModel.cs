using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GIMI_ModManager.Avalonia.Models;
using GIMI_ModManager.Avalonia.Services;
using GIMI_ModManager.Avalonia.Services.Dialogs;
using GIMI_ModManager.Avalonia.Services.Navigation;
using GIMI_ModManager.Avalonia.Services.Settings;
using GIMI_ModManager.Core.GamesService;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase, INavigationAware
{
    private const string ThemeSettingKey = "AppTheme";

    private readonly ILocalSettingsService _localSettings;
    private readonly SelectedGameService _selectedGameService;
    private readonly IGameService _gameService;
    private readonly IFilePickerService _filePicker;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly ILogger _logger;

    private bool _suppressThemeApply;

    public IReadOnlyList<GameOption> Games { get; } = GameOption.All;
    public IReadOnlyList<string> Themes { get; } = new[] { "跟随系统", "浅色", "深色" };

    [ObservableProperty] private GameOption _selectedGame = GameOption.All[0];
    [ObservableProperty] private string _selectedTheme = "跟随系统";
    [ObservableProperty] private string? _modsFolderPath;
    [ObservableProperty] private string? _xxmiFolderPath;
    [ObservableProperty] private string _gameName = string.Empty;
    [ObservableProperty] private string _appVersion = "2.29.1";

    public SettingsViewModel(ILocalSettingsService localSettings, SelectedGameService selectedGameService,
        IGameService gameService, IFilePickerService filePicker, IDialogService dialogs,
        INotificationService notifications, ILogger logger)
    {
        _localSettings = localSettings;
        _selectedGameService = selectedGameService;
        _gameService = gameService;
        _filePicker = filePicker;
        _dialogs = dialogs;
        _notifications = notifications;
        _logger = logger.ForContext<SettingsViewModel>();
    }

    public async Task OnNavigatedToAsync(object? parameter)
    {
        GameName = _gameService.GameName;

        var currentGame = await _selectedGameService.GetSelectedGameAsync();
        SelectedGame = Games.FirstOrDefault(g => g.InternalName == currentGame) ?? Games[0];

        var options = await _localSettings.ReadSettingAsync<ModManagerOptions>(ModManagerOptions.Section);
        ModsFolderPath = options?.ModsFolderPath;
        XxmiFolderPath = options?.GimiRootFolderPath;

        var savedTheme = await _localSettings.ReadSettingAsync<string>(ThemeSettingKey, SettingScope.App);
        _suppressThemeApply = true;
        SelectedTheme = savedTheme switch
        {
            "Light" => "浅色",
            "Dark" => "深色",
            _ => "跟随系统"
        };
        _suppressThemeApply = false;
    }

    partial void OnSelectedThemeChanged(string value)
    {
        if (_suppressThemeApply)
            return;

        var variant = value switch
        {
            "浅色" => ThemeVariant.Light,
            "深色" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        if (Application.Current is not null)
            Application.Current.RequestedThemeVariant = variant;

        var key = value switch { "浅色" => "Light", "深色" => "Dark", _ => "System" };
        _ = _localSettings.SaveSettingAsync(ThemeSettingKey, key, SettingScope.App);
    }

    [RelayCommand]
    private async Task ChangeModsFolder()
    {
        var path = await _filePicker.PickFolderAsync("选择 Mods 文件夹", ModsFolderPath);
        if (path is null)
            return;

        ModsFolderPath = path;
        await SaveOptionsAsync();
        await _dialogs.ShowMessageAsync("需要重启", "Mods 文件夹已更改。请重启 JASM 以应用新的路径。");
    }

    [RelayCommand]
    private async Task ChangeXxmiFolder()
    {
        var path = await _filePicker.PickFolderAsync("选择 XXMI / 3Dmigoto 文件夹", XxmiFolderPath);
        if (path is null)
            return;

        XxmiFolderPath = path;
        await SaveOptionsAsync();
        await _dialogs.ShowMessageAsync("需要重启", "XXMI 文件夹已更改。请重启 JASM 以应用新的路径。");
    }

    private async Task SaveOptionsAsync()
    {
        var options = await _localSettings.ReadOrCreateSettingAsync<ModManagerOptions>(ModManagerOptions.Section);
        options.ModsFolderPath = ModsFolderPath;
        options.GimiRootFolderPath = XxmiFolderPath;
        await _localSettings.SaveSettingAsync(ModManagerOptions.Section, options);
    }

    [RelayCommand]
    private async Task SwitchGame()
    {
        if (SelectedGame.InternalName == await _selectedGameService.GetSelectedGameAsync())
            return;

        await _selectedGameService.SetSelectedGame(SelectedGame.InternalName);
        _notifications.ShowInfo("已切换游戏", $"已切换到 {SelectedGame.DisplayName}。请重启 JASM 以加载该游戏。");
    }

    [RelayCommand]
    private void OpenModsFolder()
    {
        if (!string.IsNullOrWhiteSpace(ModsFolderPath))
            PlatformService.OpenInFileManager(ModsFolderPath);
    }

    [RelayCommand]
    private void OpenAppDataFolder() => PlatformService.OpenInFileManager(_localSettings.ApplicationDataFolder);
}
