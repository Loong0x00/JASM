using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.Services;

namespace JASM.Tests;

/// <summary>
/// Verifies every supported game's asset bundle loads on Linux and yields moddable objects.
/// This is the "5 games must all work" guard.
/// </summary>
public class AllGamesLoadTests : IDisposable
{
    private readonly string _assetsRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "GIMI-ModManager.WinUI", "Assets", "Games"));

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "jasm-allgames-" + Guid.NewGuid().ToString("N"));

    private SkinManagerService? _skinManager;

    [Theory]
    [InlineData("Genshin")]
    [InlineData("Honkai")]
    [InlineData("WuWa")]
    [InlineData("ZZZ")]
    [InlineData("Endfield")]
    public async Task Game_loads_assets_and_yields_moddable_objects(string gameFolder)
    {
        var assetsDir = Path.Combine(_assetsRoot, gameFolder);
        Assert.True(File.Exists(Path.Combine(assetsDir, "game.json")),
            $"Missing game.json for {gameFolder} at {assetsDir}");

        var settingsDir = Path.Combine(_root, gameFolder, "settings");
        var modsDir = Path.Combine(_root, gameFolder, "mods");
        Directory.CreateDirectory(settingsDir);
        Directory.CreateDirectory(modsDir);

        var logger = new MockLogger();
        var game = new GameService(logger, new MockLocalizer());
        await game.InitializeAsync(new InitializationOptions
        {
            AssetsDirectory = assetsDir,
            LocalSettingsDirectory = settingsDir
        });

        // Game metadata loaded
        Assert.False(string.IsNullOrWhiteSpace(game.GameName));
        Assert.False(string.IsNullOrWhiteSpace(game.GameShortName));

        // Categories + moddable objects present
        var categories = game.GetCategories();
        Assert.NotEmpty(categories);

        var objects = game.GetAllModdableObjects();
        Assert.NotEmpty(objects);

        // SkinManager can build a mod list per object and create the folder structure
        var crawler = new ModCrawlerService(logger, game);
        var skinManager = _skinManager = new SkinManagerService(game, logger, crawler);
        await skinManager.InitializeAsync(modsDir, null, null);
        Assert.NotEmpty(skinManager.CharacterModLists);

        // A character-category object resolves and its mod list is addressable
        var first = objects[0];
        var modList = skinManager.GetCharacterModListOrDefault(first.InternalName);
        Assert.NotNull(modList);
    }

    public void Dispose()
    {
        _skinManager?.Dispose();
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch
        {
            // best-effort
        }
    }
}
