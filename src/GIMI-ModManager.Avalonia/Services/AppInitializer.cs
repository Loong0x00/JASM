using GIMI_ModManager.Avalonia.Models;
using GIMI_ModManager.Avalonia.Services.Settings;
using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.Services.ModPresetService;
using Serilog;

namespace GIMI_ModManager.Avalonia.Services;

/// <summary>
/// Owns the one-time initialization of the Core GameService + SkinManagerService for the
/// currently selected game, using the persisted folder paths. GameService can only be
/// initialized once per process, so switching games requires an app restart (as in the
/// original WinUI build).
/// </summary>
public class AppInitializer
{
    private readonly ILocalSettingsService _localSettings;
    private readonly SelectedGameService _selectedGameService;
    private readonly IGameService _gameService;
    private readonly ISkinManagerService _skinManagerService;
    private readonly ModPresetService _presetService;
    private readonly ILogger _logger;

    public bool IsInitialized { get; private set; }
    public string? CurrentGame { get; private set; }

    public AppInitializer(ILocalSettingsService localSettings, SelectedGameService selectedGameService,
        IGameService gameService, ISkinManagerService skinManagerService, ModPresetService presetService,
        ILogger logger)
    {
        _localSettings = localSettings;
        _selectedGameService = selectedGameService;
        _gameService = gameService;
        _skinManagerService = skinManagerService;
        _presetService = presetService;
        _logger = logger.ForContext<AppInitializer>();
    }

    public Task<string> GetSelectedGameAsync() => _selectedGameService.GetSelectedGameAsync();

    public Task PrepareSelectedGameAsync() => _selectedGameService.InitializeAsync();

    public Task<bool> IsConfiguredAsync(string game) => _selectedGameService.IsJasmInitializedForGameAsync(game);

    /// <summary>Reads the saved options for the selected game and initializes the Core services.</summary>
    public async Task InitializeForCurrentGameAsync()
    {
        if (IsInitialized)
            return;

        var game = await _selectedGameService.GetSelectedGameAsync();
        var options = await _localSettings.ReadSettingAsync<ModManagerOptions>(ModManagerOptions.Section);

        if (options is null || string.IsNullOrWhiteSpace(options.ModsFolderPath))
            throw new InvalidOperationException($"JASM is not configured for {game}.");

        var assetsDir = GameAssetService.GetGameAssetsDirectory(game);

        _logger.Information("Initializing game {Game}. Assets={Assets} Mods={Mods} XXMI={Xxmi}",
            game, assetsDir, options.ModsFolderPath, options.GimiRootFolderPath);

        await _gameService.InitializeAsync(assetsDir, _localSettings.ApplicationDataFolder);

        var threeMigoto = string.IsNullOrWhiteSpace(options.GimiRootFolderPath) ||
                          !Directory.Exists(options.GimiRootFolderPath)
            ? null
            : options.GimiRootFolderPath;

        await _skinManagerService.InitializeAsync(options.ModsFolderPath!, null, threeMigoto);
        await _presetService.InitializeAsync(_localSettings.ApplicationDataFolder);

        CurrentGame = game;
        IsInitialized = true;
    }

    /// <summary>Used by the first-time setup flow, which supplies freshly picked paths.</summary>
    public async Task InitializeFromStartupAsync(string game, string modsFolder, string? xxmiFolder)
    {
        var assetsDir = GameAssetService.GetGameAssetsDirectory(game);
        await _gameService.InitializeAsync(assetsDir, _localSettings.ApplicationDataFolder);

        var threeMigoto = string.IsNullOrWhiteSpace(xxmiFolder) || !Directory.Exists(xxmiFolder)
            ? null
            : xxmiFolder;

        await _skinManagerService.InitializeAsync(modsFolder, null, threeMigoto);
        await _presetService.InitializeAsync(_localSettings.ApplicationDataFolder);

        CurrentGame = game;
        IsInitialized = true;
    }
}
