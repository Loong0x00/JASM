using CommunityToolkit.Mvvm.ComponentModel;
using GIMI_ModManager.Core.Entities.Mods.Contract;

namespace GIMI_ModManager.Avalonia.ViewModels.Items;

/// <summary>Editable view of one 3Dmigoto key-swap section (skin-cycling keys) inside a mod's .ini.</summary>
public partial class KeySwapItem : ObservableObject
{
    public string IniFile { get; }
    public string SectionName { get; }
    public string? Type { get; }
    public int? Variants { get; }

    [ObservableProperty] private string? _forwardKey;
    [ObservableProperty] private string? _backwardKey;

    public KeySwapItem(string iniFile, KeySwapSection section)
    {
        IniFile = iniFile;
        SectionName = section.SectionName;
        Type = section.Type;
        Variants = section.Variants;
        _forwardKey = section.ForwardKey;
        _backwardKey = section.BackwardKey;
    }

    public KeySwapSection ToSection() => new()
    {
        SectionName = SectionName,
        OriginalSectionName = SectionName,
        Type = Type,
        Variants = Variants,
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
