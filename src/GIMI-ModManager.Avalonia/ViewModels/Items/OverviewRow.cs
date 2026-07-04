using GIMI_ModManager.Core.GamesService.Interfaces;

namespace GIMI_ModManager.Avalonia.ViewModels.Items;

public class OverviewRow
{
    public IModdableObject Character { get; }
    public string CharacterName => Character.DisplayName;
    public string? CharacterImagePath => Character.ImageUri?.LocalPath;
    public string ModName { get; }
    public bool IsEnabled { get; }
    public string FolderPath { get; }

    public string StatusText => IsEnabled ? "已启用" : "已禁用";

    public OverviewRow(IModdableObject character, string modName, bool isEnabled, string folderPath)
    {
        Character = character;
        ModName = modName;
        IsEnabled = isEnabled;
        FolderPath = folderPath;
    }
}
