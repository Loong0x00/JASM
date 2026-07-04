using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.GamesService;
using GIMI_ModManager.Core.GamesService.Interfaces;
using GIMI_ModManager.Core.Helpers;
using GIMI_ModManager.Core.Services;

namespace JASM.Tests;

/// <summary>
/// End-to-end verification that the mod enable/disable and delete mechanism works on Linux
/// against a real on-disk XXMI-style folder layout. Also exercises the Linux-specific patches
/// in <c>Mod.cs</c> (XDG-trash delete instead of the Windows recycle bin).
/// </summary>
public class LinuxModToggleTests : IDisposable
{
    private readonly string _assetsPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..",
        "GIMI-ModManager.WinUI", "Assets", "Games", "Genshin"));

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "jasm-linux-test-" + Guid.NewGuid().ToString("N"));

    private string ModsFolder => Path.Combine(_root, "Mods");
    private string SettingsFolder => Path.Combine(_root, "Settings");

    private async Task<(GameService game, SkinManagerService skinManager)> InitAsync()
    {
        Directory.CreateDirectory(ModsFolder);
        Directory.CreateDirectory(SettingsFolder);

        var logger = new MockLogger();
        var game = new GameService(logger, new MockLocalizer());
        await game.InitializeAsync(new InitializationOptions
        {
            AssetsDirectory = _assetsPath,
            LocalSettingsDirectory = SettingsFolder
        });

        var crawler = new ModCrawlerService(logger, game);
        var skinManager = new SkinManagerService(game, logger, crawler);
        await skinManager.InitializeAsync(ModsFolder, null, null);
        return (game, skinManager);
    }

    private void SeedMod(string characterInternalName, string modFolderName)
    {
        var modDir = Path.Combine(ModsFolder, "character", characterInternalName, modFolderName);
        Directory.CreateDirectory(modDir);
        File.WriteAllText(Path.Combine(modDir, "merged.ini"), "[Constants]\n");
        File.WriteAllText(Path.Combine(modDir, "texture.dds"), "fake");
    }

    [Fact]
    public async Task Toggling_a_mod_renames_the_folder_with_the_DISABLED_prefix()
    {
        SeedMod("albedo", "CoolAlbedo");
        var (game, skinManager) = await InitAsync();

        var character = game.GetCharacterByIdentifier("Albedo");
        Assert.NotNull(character);

        var modList = skinManager.GetCharacterModList(character!);
        var mods = modList.Mods;
        Assert.Single(mods);

        var entry = mods.First();
        Assert.True(entry.IsEnabled, "Freshly scanned mod should be enabled");
        Assert.True(Directory.Exists(Path.Combine(ModsFolder, "character", "albedo", "CoolAlbedo")));

        // Disable → folder gets the DISABLED_ prefix
        var nowEnabled = modList.ToggleMod(entry.Id);
        Assert.False(nowEnabled);
        Assert.False(Directory.Exists(Path.Combine(ModsFolder, "character", "albedo", "CoolAlbedo")));
        Assert.True(Directory.Exists(Path.Combine(ModsFolder, "character", "albedo",
            ModFolderHelpers.DISABLED_PREFIX + "CoolAlbedo")));

        // Re-enable → prefix removed
        var reEnabled = modList.ToggleMod(entry.Id);
        Assert.True(reEnabled);
        Assert.True(Directory.Exists(Path.Combine(ModsFolder, "character", "albedo", "CoolAlbedo")));
    }

    [Fact]
    public async Task Deleting_a_mod_to_recycle_bin_moves_it_to_the_xdg_trash_on_linux()
    {
        // Point XDG_DATA_HOME at an isolated trash dir so we don't touch the real trash.
        var trashHome = Path.Combine(_root, "xdg");
        Directory.CreateDirectory(trashHome);
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", trashHome);

        SeedMod("amber", "RedAmber");
        var (game, skinManager) = await InitAsync();

        var character = game.GetCharacterByIdentifier("Amber");
        var modList = skinManager.GetCharacterModList(character!);
        var entry = modList.Mods.Single();

        modList.DeleteModBySkinEntryId(entry.Id, moveToRecycleBin: true);

        // Gone from the mods folder...
        Assert.False(Directory.Exists(Path.Combine(ModsFolder, "character", "amber", "RedAmber")));
        // ...and recoverable in the XDG trash (files + .trashinfo).
        Assert.True(Directory.Exists(Path.Combine(trashHome, "Trash", "files", "RedAmber")));
        Assert.True(File.Exists(Path.Combine(trashHome, "Trash", "info", "RedAmber.trashinfo")));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", null);
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
