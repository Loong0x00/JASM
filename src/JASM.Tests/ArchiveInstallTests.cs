using System.IO.Compression;
using GIMI_ModManager.Core.Entities.Mods.SkinMod;
using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.Services;

namespace JASM.Tests;

/// <summary>
/// Verifies installing a mod from a .zip archive works on Linux: extraction goes through
/// SharpCompress (the bundled Windows 7-zip is unavailable), then the mod is added to a character.
/// </summary>
public class ArchiveInstallTests : IDisposable
{
    private readonly string _assetsPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "GIMI-ModManager.WinUI", "Assets", "Games", "Genshin"));

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "jasm-archive-" + Guid.NewGuid().ToString("N"));

    private SkinManagerService? _skinManager;

    [Fact]
    public async Task Installing_a_mod_from_a_zip_extracts_and_adds_it_to_the_character()
    {
        Directory.CreateDirectory(_root);

        // Build a source mod and zip it.
        var srcMod = Path.Combine(_root, "CoolGanyuMod");
        Directory.CreateDirectory(srcMod);
        await File.WriteAllTextAsync(Path.Combine(srcMod, "merged.ini"), "[Constants]\n");
        await File.WriteAllTextAsync(Path.Combine(srcMod, "texture.dds"), "fake");

        var zipPath = Path.Combine(_root, "CoolGanyuMod.zip");
        ZipFile.CreateFromDirectory(srcMod, zipPath);

        // Extract via Core ArchiveService (SharpCompress path on Linux).
        var logger = new MockLogger();
        var archiveService = new ArchiveService(logger);
        var extracted = archiveService.ExtractArchive(zipPath, Path.Combine(_root, "extract"));
        Assert.True(File.Exists(Path.Combine(extracted.FullName, "merged.ini")),
            "Archive should extract the mod's ini");

        // Initialize game + skin manager and install the extracted mod.
        var modsFolder = Path.Combine(_root, "Mods");
        var settingsFolder = Path.Combine(_root, "Settings");
        Directory.CreateDirectory(modsFolder);
        Directory.CreateDirectory(settingsFolder);

        var game = new GameService(logger, new MockLocalizer());
        await game.InitializeAsync(new InitializationOptions
        {
            AssetsDirectory = _assetsPath,
            LocalSettingsDirectory = settingsFolder
        });

        var crawler = new ModCrawlerService(logger, game);
        var skinManager = _skinManager = new SkinManagerService(game, logger, crawler);
        await skinManager.InitializeAsync(modsFolder, null, null);

        var character = game.GetCharacterByIdentifier("Ganyu");
        Assert.NotNull(character);

        var modList = skinManager.GetCharacterModList(character!);
        Assert.Empty(modList.Mods);

        var mod = await SkinMod.CreateModAsync(extracted.FullName);
        skinManager.AddMod(mod, modList, move: false);

        Assert.Single(modList.Mods);
        Assert.True(Directory.Exists(Path.Combine(modsFolder, "character", "ganyu", "CoolGanyuMod")),
            "Installed mod folder should exist under the character");
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
