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

    [ObservableProperty] private int _modCount;
    [ObservableProperty] private int _enabledModCount;

    public bool HasMods => ModCount > 0;
    public bool HasEnabledMods => EnabledModCount > 0;

    public CharacterGridItem(IModdableObject moddableObject)
    {
        ModdableObject = moddableObject;
    }

    partial void OnModCountChanged(int value) => OnPropertyChanged(nameof(HasMods));

    partial void OnEnabledModCountChanged(int value) => OnPropertyChanged(nameof(HasEnabledMods));
}
