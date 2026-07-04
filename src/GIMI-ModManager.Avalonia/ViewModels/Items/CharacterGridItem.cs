using CommunityToolkit.Mvvm.ComponentModel;
using GIMI_ModManager.Core.GamesService.Interfaces;

namespace GIMI_ModManager.Avalonia.ViewModels.Items;

public partial class CharacterGridItem : ObservableObject
{
    public IModdableObject ModdableObject { get; }

    public string DisplayName => ModdableObject.DisplayName;
    public string InternalName => ModdableObject.InternalName;
    public string? ImagePath => ModdableObject.ImageUri?.LocalPath;
    public string CategoryName => ModdableObject.ModCategory.DisplayNamePlural;

    // Character-only metadata (null / 0 for weapons, objects, npcs).
    public string? ElementName { get; }
    public string? ElementImagePath { get; }
    public string? ClassName { get; }
    public int Rarity { get; }
    public DateTime? ReleaseDate { get; }

    [ObservableProperty] private int _modCount;
    [ObservableProperty] private int _enabledModCount;
    [ObservableProperty] private bool _isPinned;
    [ObservableProperty] private bool _isHidden;

    public bool HasMods => ModCount > 0;
    public bool HasEnabledMods => EnabledModCount > 0;
    public string PinGlyph => IsPinned ? "★" : "☆";

    public CharacterGridItem(IModdableObject moddableObject)
    {
        ModdableObject = moddableObject;

        if (moddableObject is ICharacter character)
        {
            ElementName = character.Element.DisplayName;
            ElementImagePath = character.Element.ImageUri?.LocalPath;
            ClassName = character.Class.DisplayName;
            Rarity = character.Rarity;
            ReleaseDate = character.ReleaseDate;
        }
    }

    partial void OnModCountChanged(int value) => OnPropertyChanged(nameof(HasMods));

    partial void OnEnabledModCountChanged(int value) => OnPropertyChanged(nameof(HasEnabledMods));

    partial void OnIsPinnedChanged(bool value) => OnPropertyChanged(nameof(PinGlyph));
}
