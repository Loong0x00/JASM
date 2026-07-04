using GIMI_ModManager.Avalonia.Models;
using GIMI_ModManager.Avalonia.Services.Settings;
using GIMI_ModManager.Core.GamesService;
using Newtonsoft.Json;
using Serilog;

namespace GIMI_ModManager.Avalonia.Services;

/// <summary>
/// Tracks which game the user is currently managing and points the settings service at that
/// game's profile folder. Persists the choice to JASM/game.json (same file the Windows build uses).
/// </summary>
public class SelectedGameService
{
    private readonly ILocalSettingsService _localSettingsService;
    private readonly ILogger _logger;

    private const string DefaultApplicationDataFolder = "ApplicationData";

    private readonly string _jasmAppDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JASM");

    private const string ConfigFile = "game.json";
    private readonly string _configPath;

    public const string Genshin = "Genshin";

    public SelectedGameService(ILocalSettingsService localSettingsService, ILogger logger)
    {
        _localSettingsService = localSettingsService;
        _logger = logger.ForContext<SelectedGameService>();
        Directory.CreateDirectory(_jasmAppDataPath);
        _configPath = Path.Combine(_jasmAppDataPath, ConfigFile);
    }

    private static string GetGameSpecificSettingsFolderName(string game) =>
        DefaultApplicationDataFolder + "_" + game;

    public async Task SetSelectedGame(string game)
    {
        if (!IsValidGame(game))
            throw new ArgumentException($"Invalid game name: {game}");

        _localSettingsService.SetApplicationDataFolderName(GetGameSpecificSettingsFolderName(game));
        await SaveSelectedGameAsync(game).ConfigureAwait(false);
    }

    public async Task InitializeAsync()
    {
        if (!File.Exists(_configPath))
            await SaveSelectedGameAsync(Genshin).ConfigureAwait(false);

        var selectedGame = await GetSelectedGameAsync().ConfigureAwait(false);
        _localSettingsService.SetApplicationDataFolderName(GetGameSpecificSettingsFolderName(selectedGame));
    }

    public async Task<string> GetSelectedGameAsync()
    {
        if (!File.Exists(_configPath))
            return Genshin;

        try
        {
            var selectedGame =
                JsonConvert.DeserializeObject<SelectedGameModel>(await File.ReadAllTextAsync(_configPath));

            if (selectedGame is null || !IsValidGame(selectedGame.SelectedGame))
                return Genshin;

            return selectedGame.SelectedGame;
        }
        catch (Exception e)
        {
            _logger.Warning(e, "Failed to read selected game, defaulting to {Game}", Genshin);
            return Genshin;
        }
    }

    public Task SaveSelectedGameAsync(string game)
    {
        if (!IsValidGame(game))
            throw new ArgumentException($"Invalid game name: {game}");

        var selectedGame = new SelectedGameModel { SelectedGame = game };
        return File.WriteAllTextAsync(_configPath, JsonConvert.SerializeObject(selectedGame, Formatting.Indented));
    }

    /// <summary>
    /// True if JASM already has the required folder paths configured for the given game.
    /// Temporarily re-points the settings folder to read that game's profile.
    /// </summary>
    public async Task<bool> IsJasmInitializedForGameAsync(string game)
    {
        if (!IsValidGame(game))
            throw new ArgumentException($"Invalid game name: {game}");

        var currentGame = await GetSelectedGameAsync().ConfigureAwait(false);
        var switched = !string.Equals(currentGame, game, StringComparison.OrdinalIgnoreCase);

        if (switched)
            _localSettingsService.SetApplicationDataFolderName(GetGameSpecificSettingsFolderName(game));

        var options = await _localSettingsService.ReadSettingAsync<ModManagerOptions>(ModManagerOptions.Section)
            .ConfigureAwait(false);

        // On Linux the XXMI/3Dmigoto loader folder is optional (JASM only organizes mod files),
        // so a valid Mods folder alone counts as configured.
        var initialized = options is not null &&
                          !string.IsNullOrEmpty(options.ModsFolderPath) &&
                          Directory.Exists(options.ModsFolderPath);

        if (switched)
            _localSettingsService.SetApplicationDataFolderName(GetGameSpecificSettingsFolderName(currentGame));

        return initialized;
    }

    public static bool IsValidGame(string game) => Enum.TryParse<SupportedGames>(game, out _);

    public static IReadOnlyList<SupportedGames> AllGames { get; } =
        Enum.GetValues<SupportedGames>();
}
