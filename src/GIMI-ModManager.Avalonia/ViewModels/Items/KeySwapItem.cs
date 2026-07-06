using CommunityToolkit.Mvvm.ComponentModel;
using GIMI_ModManager.Core.Entities.Mods.Contract;

namespace GIMI_ModManager.Avalonia.ViewModels.Items;

/// <summary>One 3Dmigoto key-swap section (skin-cycling keys) inside a mod's .ini.</summary>
public partial class KeySwapItem : ObservableObject
{
    public string IniFile { get; }
    public string SectionName { get; }
    public string? Type { get; }
    public int? Variants { get; }

    /// <summary>The mod's on-screen-character variable (e.g. <c>$object_detected</c>), or null if it has none.</summary>
    public string? ActiveVar { get; }

    private readonly string? _originalCondition;

    [ObservableProperty] private string? _forwardKey;
    [ObservableProperty] private string? _backwardKey;

    /// <summary>Gate state, driven by the pane's mod-level switch; feeds <see cref="EffectiveCondition"/>.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustom))]
    [NotifyPropertyChangedFor(nameof(IsSwitchable))]
    private KeySwapState _state;

    /// <summary>Author-written condition (menu/GUI logic) — shown read-only; the mod switch leaves it alone.</summary>
    public bool IsCustom => State == KeySwapState.Custom;

    /// <summary>Only plain skin-cycle keys follow the mod-level switch; author logic does not.</summary>
    public bool IsSwitchable => State != KeySwapState.Custom;

    public KeySwapItem(string iniFile, KeySwapSection section, string? activeVar = null)
    {
        IniFile = iniFile;
        SectionName = section.SectionName;
        Type = section.Type;
        Variants = section.Variants;
        ActiveVar = activeVar;
        _originalCondition = section.Condition;
        _forwardKey = section.ForwardKey;
        _backwardKey = section.BackwardKey;
        _state = KeySwapStateHelper.Classify(section.Condition);
    }

    /// <summary>The <c>condition</c> value the current state maps to. Custom keeps the author's original.</summary>
    public string? EffectiveCondition => State switch
    {
        KeySwapState.ForegroundOnly => ActiveVar,
        KeySwapState.Disabled => KeySwapStateHelper.DisabledCondition,
        KeySwapState.Unrestricted => null,
        _ => _originalCondition // Custom
    };

    public KeySwapSection ToSection() => new()
    {
        SectionName = SectionName,
        OriginalSectionName = SectionName,
        Type = Type,
        Variants = Variants,
        Condition = EffectiveCondition,
        ForwardKey = ForwardKey,
        BackwardKey = BackwardKey,
        ForwardKeys = Split(ForwardKey),
        BackwardKeys = Split(BackwardKey)
    };

    private static List<string> Split(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
}
