namespace GIMI_ModManager.Avalonia.Services;

/// <summary>
/// Resolves where a game's bundled asset data (characters.json, images, ...) lives on disk.
/// The assets are copied next to the executable under Assets/Games/&lt;Game&gt;/.
/// </summary>
public static class GameAssetService
{
    public static string GetGameAssetsDirectory(string gameName) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "Games", gameName);

    public static bool HasAssetsFor(string gameName) =>
        File.Exists(Path.Combine(GetGameAssetsDirectory(gameName), "game.json"));
}
