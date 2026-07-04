using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.Services;
using GIMI_ModManager.Core.Services.ModPresetService;

namespace JASM.Tests;

/// <summary>
/// Verifies the mod-preset round trip on Linux: snapshot the enabled mods into a preset, disable
/// everything, then apply the preset and confirm the original enabled set is restored (which works
/// by renaming the DISABLED_ folder prefixes).
/// </summary>
public class PresetRoundTripTests : IDisposable
{
    private readonly string _assetsPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "GIMI-ModManager.WinUI", "Assets", "Games", "Genshin"));

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "jasm-preset-" + Guid.NewGuid().ToString("N"));

    private string ModsFolder => Path.Combine(_root, "Mods");
    private string SettingsFolder => Path.Combine(_root, "Settings");

    private SkinManagerService? _skinManager;
    private ModPresetService? _presetService;

    private void SeedMod(string character, string modFolderName)
    {
        var dir = Path.Combine(ModsFolder, "character", character, modFolderName);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "merged.ini"), "[Constants]\n");
    }

    [Fact]
    public async Task Applying_a_preset_restores_the_snapshotted_enabled_mods()
    {
        Directory.CreateDirectory(ModsFolder);
        Directory.CreateDirectory(SettingsFolder);

        // Ganyu: one enabled, one disabled to start.
        SeedMod("ganyu", "GanyuA");
        SeedMod("ganyu", "DISABLED_GanyuB");

        var logger = new MockLogger();
        var game = new GameService(logger, new MockLocalizer());
        await game.InitializeAsync(new InitializationOptions
        {
            AssetsDirectory = _assetsPath,
            LocalSettingsDirectory = SettingsFolder
        });

        var crawler = new ModCrawlerService(logger, game);
        var skinManager = _skinManager = new SkinManagerService(game, logger, crawler);
        await skinManager.InitializeAsync(ModsFolder, null, null);

        var presetService = _presetService = new ModPresetService(logger, skinManager);
        await presetService.InitializeAsync(SettingsFolder);

        var ganyu = game.GetCharacterByIdentifier("Ganyu")!;
        var modList = skinManager.GetCharacterModList(ganyu);
        var enabledA = modList.Mods.Single(m => m.Mod.GetDisplayName() == "GanyuA");
        Assert.True(enabledA.IsEnabled);

        // Snapshot current state (GanyuA enabled) into a preset.
        await presetService.CreatePresetAsync("MyLook", createEmptyPreset: false);

        // Now disable GanyuA.
        modList.DisableMod(enabledA.Id);
        Assert.False(modList.Mods.Single(m => m.Id == enabledA.Id).IsEnabled);

        // Apply the preset → GanyuA should be re-enabled.
        await presetService.ApplyPresetAsync("MyLook");

        var after = skinManager.GetCharacterModList(ganyu).Mods.Single(m => m.Id == enabledA.Id);
        Assert.True(after.IsEnabled, "Applying the preset should re-enable the snapshotted mod");
    }

    public void Dispose()
    {
        _presetService?.Dispose();
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
