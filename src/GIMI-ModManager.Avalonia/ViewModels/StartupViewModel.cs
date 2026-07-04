using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using GIMI_ModManager.Avalonia.Models;
using GIMI_ModManager.Avalonia.Services;
using GIMI_ModManager.Avalonia.Services.Dialogs;
using GIMI_ModManager.Avalonia.Services.Settings;
using Serilog;

namespace GIMI_ModManager.Avalonia.ViewModels;

public partial class StartupViewModel : ViewModelBase
{
    private readonly ILocalSettingsService _localSettings;
    private readonly SelectedGameService _selectedGameService;
    private readonly AppInitializer _appInitializer;
    private readonly IFilePickerService _filePicker;
    private readonly IDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly IMessenger _messenger;
    private readonly ILogger _logger;

    public IReadOnlyList<GameOption> Games { get; } = GameOption.All;

    [ObservableProperty] private GameOption _selectedGame = GameOption.All[0];
    [ObservableProperty] private string? _modsFolderPath;
    [ObservableProperty] private string? _xxmiFolderPath;
    [ObservableProperty] private bool _isBusy;

    public StartupViewModel(ILocalSettingsService localSettings, SelectedGameService selectedGameService,
        AppInitializer appInitializer, IFilePickerService filePicker, IDialogService dialogs,
        INotificationService notifications, IMessenger messenger, ILogger logger)
    {
        _localSettings = localSettings;
        _selectedGameService = selectedGameService;
        _appInitializer = appInitializer;
        _filePicker = filePicker;
        _dialogs = dialogs;
        _notifications = notifications;
        _messenger = messenger;
        _logger = logger.ForContext<StartupViewModel>();
    }

    [RelayCommand]
    private async Task BrowseModsFolder()
    {
        var path = await _filePicker.PickFolderAsync("选择 Mods 文件夹", ModsFolderPath);
        if (path is not null)
            ModsFolderPath = path;
    }

    [RelayCommand]
    private async Task BrowseXxmiFolder()
    {
        var path = await _filePicker.PickFolderAsync("选择 XXMI / 3Dmigoto 文件夹", XxmiFolderPath);
        if (path is not null)
            XxmiFolderPath = path;
    }

    [RelayCommand]
    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(ModsFolderPath) || !Directory.Exists(ModsFolderPath))
        {
            await _dialogs.ShowMessageAsync("缺少 Mods 文件夹", "请选择一个有效的 Mods 文件夹。JASM 会在其中整理模组。");
            return;
        }

        if (!string.IsNullOrWhiteSpace(XxmiFolderPath) &&
            string.Equals(Path.GetFullPath(XxmiFolderPath), Path.GetFullPath(ModsFolderPath),
                StringComparison.OrdinalIgnoreCase))
        {
            await _dialogs.ShowMessageAsync("路径冲突", "Mods 文件夹和 XXMI 文件夹不能是同一个目录。");
            return;
        }

        // The XXMI/3Dmigoto loader folder is optional on Linux (JASM only organizes mod files),
        // but warn if it doesn't look like a real loader folder.
        if (!string.IsNullOrWhiteSpace(XxmiFolderPath) &&
            !File.Exists(Path.Combine(XxmiFolderPath, "d3dx.ini")))
        {
            var proceed = await _dialogs.ShowConfirmAsync("未找到 d3dx.ini",
                "所选 XXMI 文件夹中没有 d3dx.ini，可能不是有效的加载器目录。仍要继续吗？",
                "继续", "返回");
            if (!proceed)
                return;
        }

        IsBusy = true;
        try
        {
            var options = new ModManagerOptions
            {
                ModsFolderPath = ModsFolderPath,
                GimiRootFolderPath = string.IsNullOrWhiteSpace(XxmiFolderPath) ? null : XxmiFolderPath
            };

            await _selectedGameService.SetSelectedGame(SelectedGame.InternalName);
            await _localSettings.SaveSettingAsync(ModManagerOptions.Section, options);
            await _appInitializer.InitializeFromStartupAsync(SelectedGame.InternalName, ModsFolderPath, XxmiFolderPath);

            _notifications.ShowSuccess("配置完成", $"已为 {SelectedGame.DisplayName} 初始化 JASM。");
            _messenger.Send(new StartupCompletedMessage());
        }
        catch (Exception e)
        {
            _logger.Error(e, "First-time setup failed");
            await _dialogs.ShowMessageAsync("初始化失败", e.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public sealed record GameOption(string InternalName, string DisplayName)
{
    public static IReadOnlyList<GameOption> All { get; } =
    [
        new("Genshin", "原神 (GIMI)"),
        new("Honkai", "崩坏：星穹铁道 (SRMI)"),
        new("WuWa", "鸣潮 (WWMI)"),
        new("ZZZ", "绝区零 (ZZMI)"),
        new("Endfield", "明日方舟：终末地 (EFMI)")
    ];

    public override string ToString() => DisplayName;
}
