using GIMI_ModManager.Core.Services.ModPresetService.Models;

namespace GIMI_ModManager.Avalonia.ViewModels.Items;

public class PresetItem
{
    public ModPreset Preset { get; }
    public string Name => Preset.Name;
    public int ModCount => Preset.Mods.Count;
    public bool IsReadOnly => Preset.IsReadOnly;
    public DateTime Created => Preset.Created;

    public PresetItem(ModPreset preset)
    {
        Preset = preset;
    }
}
