using GIMI_ModManager.Core.Entities.Mods.FileModels;

namespace GIMI_ModManager.Core.Entities.Mods.Contract;

public record KeySwapSection
{
    public string SectionName { get; init; } = "Unknown";

    public string? ForwardKey { get; init; }

    public string? BackwardKey { get; init; }

    public int? Variants { get; init; }

    public string? Type { get; init; }

    public List<string> ForwardKeys { get; init; } = [];

    public List<string> BackwardKeys { get; init; } = [];

    /// <summary>
    /// The 3dmigoto <c>condition</c> gate. See <see cref="IniKeySwapSection.Condition"/>. Null when the
    /// key has no condition (fires globally). Use <see cref="KeySwapStateHelper"/> to map to a state.
    /// </summary>
    public string? Condition { get; init; }

    public string? OriginalSectionName { get; init; }
    internal static KeySwapSection FromIniKeySwapSection(IniKeySwapSection iniKeySwapSection)
    {
        return new KeySwapSection
        {
            SectionName = iniKeySwapSection.SectionKey,
            ForwardKey = string.Join(", ", iniKeySwapSection.ForwardKeys),
            BackwardKey = string.Join(", ", iniKeySwapSection.BackwardKeys),
            ForwardKeys = [.. iniKeySwapSection.ForwardKeys],
            BackwardKeys = [.. iniKeySwapSection.BackwardKeys],
            Variants = iniKeySwapSection.SwapVar?.Length,
            Type = iniKeySwapSection.Type ?? "",
            Condition = iniKeySwapSection.Condition,
        };
    }
}

/// <summary>
/// The three user-facing states a skin-cycle key can be put into, plus <see cref="Custom"/> for
/// author-written conditions JASM must not clobber.
/// </summary>
public enum KeySwapState
{
    /// <summary>No condition — the key fires for every mod bound to it (the "one key toggles all" default).</summary>
    Unrestricted,

    /// <summary>Gated on the mod's character-active variable — only cycles the on-screen character.</summary>
    ForegroundOnly,

    /// <summary>Gated on a never-true constant (<c>condition = 0</c>) — the key does nothing.</summary>
    Disabled,

    /// <summary>A non-trivial author-written condition (menu/GUI logic etc.). JASM leaves these alone.</summary>
    Custom
}

public static class KeySwapStateHelper
{
    /// <summary>The never-true constant used to disable a key without commenting the section out.</summary>
    public const string DisabledCondition = "0";

    /// <summary>Classify a keyswap's current <c>condition</c> into a <see cref="KeySwapState"/>.</summary>
    public static KeySwapState Classify(string? condition)
    {
        var c = condition?.Trim();
        if (string.IsNullOrEmpty(c) || c is "1")
            return KeySwapState.Unrestricted;

        if (c is "0" || c.Equals("false", StringComparison.OrdinalIgnoreCase))
            return KeySwapState.Disabled;

        // A condition that is *only* a reference to the character-active variable (optionally "== 1")
        // is the standard "foreground only" gate. Anything more complex is author logic → Custom.
        if (IsSimpleActiveVarCondition(c))
            return KeySwapState.ForegroundOnly;

        return KeySwapState.Custom;
    }

    /// <summary>True if the condition is just an on-screen-character check we can safely toggle.</summary>
    public static bool IsSimpleActiveVarCondition(string? condition)
    {
        var c = condition?.Trim();
        if (string.IsNullOrEmpty(c)) return false;
        // Reject anything with boolean/extra logic (menu, hover, &&, ||, etc.)
        if (c.Contains("&&") || c.Contains("||")) return false;
        var lower = c.ToLowerInvariant();
        // Must reference a known active var and nothing menu/gui related.
        if (lower.Contains("menu") || lower.Contains("gui") || lower.Contains("hover")) return false;
        return (lower.Contains("$object_detected") || lower.Contains("$active")) &&
               // only "$var", "$var == 1", "$var==1" shapes
               System.Text.RegularExpressions.Regex.IsMatch(c, @"^\$\w+\s*(==\s*1)?$");
    }
}