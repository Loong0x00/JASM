using GIMI_ModManager.Core.Entities.Mods.Contract;
using GIMI_ModManager.Core.Entities.Mods.SkinMod;
using Xunit;

namespace JASM.Tests;

public class KeySwapNormalizeTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jasm_keyswap_" + Guid.NewGuid().ToString("N"));

    public KeySwapNormalizeTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { /* best effort */ }
    }

    [Theory]
    [InlineData(null, KeySwapState.Unrestricted)]
    [InlineData("1", KeySwapState.Unrestricted)]
    [InlineData("0", KeySwapState.Disabled)]
    [InlineData("false", KeySwapState.Disabled)]
    [InlineData("$object_detected", KeySwapState.ForegroundOnly)]
    [InlineData("$active == 1", KeySwapState.ForegroundOnly)]
    [InlineData("$menu == 1 && $hovering > 1", KeySwapState.Custom)]
    [InlineData("$object_detected && $menu == 0", KeySwapState.Custom)]
    public void Classify_maps_condition_to_state(string? condition, KeySwapState expected)
    {
        Assert.Equal(expected, KeySwapStateHelper.Classify(condition));
    }

    [Fact]
    public async Task Normalize_gates_unrestricted_key_to_foreground_and_leaves_menu_key_alone()
    {
        var modDir = Path.Combine(_root, "TestMod");
        Directory.CreateDirectory(modDir);
        var iniPath = Path.Combine(modDir, "mod.ini");
        await File.WriteAllTextAsync(iniPath, string.Join("\n",
            "[Constants]",
            "global $object_detected = 0",
            "global $swapvar = 0",
            "",
            "; a plain skin-cycle key with NO condition -> should be gated to foreground",
            "[KeySwap]",
            "key = /",
            "back = .",
            "type = cycle",
            "$swapvar = 0,1",
            "",
            "; an author menu key with real logic -> must be left untouched",
            "[KeyMenu]",
            "condition = $menu == 1 && $hovering > 1",
            "key = m",
            "type = cycle",
            "$menu = 0,1",
            ""));

        var mod = await SkinMod.CreateModAsync(modDir);
        var changed = await mod.KeySwaps!.NormalizeUnrestrictedToForegroundAsync(backup: false);

        Assert.Equal(1, changed); // only [KeySwap] changed

        var sections = (await mod.KeySwaps.ReadAllKeySwapConfigurations())
            .Values.SelectMany(x => x).ToList();
        var swap = sections.First(s => s.SectionName.Equals("[KeySwap]", StringComparison.OrdinalIgnoreCase));
        var menu = sections.First(s => s.SectionName.Equals("[KeyMenu]", StringComparison.OrdinalIgnoreCase));

        // unrestricted -> foreground condition added, keys preserved
        Assert.Equal("$object_detected", swap.Condition);
        Assert.Equal(KeySwapState.ForegroundOnly, KeySwapStateHelper.Classify(swap.Condition));
        Assert.Contains("/", swap.ForwardKeys);
        Assert.Contains(".", swap.BackwardKeys);

        // author menu condition preserved verbatim
        Assert.Equal("$menu == 1 && $hovering > 1", menu.Condition);

        // idempotent: a second pass changes nothing
        Assert.Equal(0, await mod.KeySwaps.NormalizeUnrestrictedToForegroundAsync(backup: false));
    }

    [Fact]
    public async Task Normalize_skips_mod_without_active_variable()
    {
        var modDir = Path.Combine(_root, "GlobalFxMod");
        Directory.CreateDirectory(modDir);
        await File.WriteAllTextAsync(Path.Combine(modDir, "mod.ini"), string.Join("\n",
            "[Constants]",
            "global $toggle = 0",
            "",
            "[KeyToggle]",
            "key = \\",
            "type = cycle",
            "$toggle = 0,1",
            ""));

        var mod = await SkinMod.CreateModAsync(modDir);
        // no $object_detected/$active -> foreground isn't possible -> nothing changed
        Assert.Equal(0, await mod.KeySwaps!.NormalizeUnrestrictedToForegroundAsync(backup: false));
    }
}
