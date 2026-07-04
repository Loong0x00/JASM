using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.Services;

namespace JASM.Tests;

/// <summary>
/// Verifies exporting the mod library to a folder works on Linux (zip:true is not implemented in
/// Core, so the app uses zip:false with the character folder structure preserved).
/// </summary>
public class ExportModsTests : IDisposable
{
    private readonly string _assetsPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "GIMI-ModManager.WinUI", "Assets", "Games", "Genshin"));

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "jasm-export-" + Guid.NewGuid().ToString("N"));

    private SkinManagerService? _skinManager;

    [Fact]
    public async Task Exporting_mods_copies_them_into_the_destination_folder()
    {
        var modsFolder = Path.Combine(_root, "Mods");
        var settingsFolder = Path.Combine(_root, "Settings");
        var exportFolder = Path.Combine(_root, "Export");
        Directory.CreateDirectory(modsFolder);
        Directory.CreateDirectory(settingsFolder);
        Directory.CreateDirectory(exportFolder);

        var modDir = Path.Combine(modsFolder, "character", "ganyu", "GanyuMod");
        Directory.CreateDirectory(modDir);
        await File.WriteAllTextAsync(Path.Combine(modDir, "merged.ini"), "[Constants]\n");

        var logger = new MockLogger();
        var game = new GameService(logger, new MockLocalizer());
        await game.InitializeAsync(new InitializationOptions
        {
            AssetsDirectory = _assetsPath,
            LocalSettingsDirectory = settingsFolder
        });

        var crawler = new ModCrawlerService(logger, game);
        var skinManager = _skinManager = new SkinManagerService(game, logger, crawler);
        await skinManager.InitializeAsync(modsFolder, null, null);

        skinManager.ExportMods(
            skinManager.CharacterModLists.ToList(),
            exportFolder,
            removeLocalJasmSettings: false,
            zip: false,
            keepCharacterFolderStructure: true,
            setModStatus: SetModStatus.KeepCurrent);

        // The exported tree should contain the mod's ini somewhere under the destination.
        var exportedInis = Directory.GetFiles(exportFolder, "merged.ini", SearchOption.AllDirectories);
        Assert.NotEmpty(exportedInis);
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
