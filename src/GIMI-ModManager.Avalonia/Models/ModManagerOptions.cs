using Newtonsoft.Json;

namespace GIMI_ModManager.Avalonia.Models;

/// <summary>
/// Persisted per-game configuration: where the mods live and where the XXMI / 3Dmigoto loader is.
/// Mirrors the WinUI app's option so existing JASM settings files stay compatible.
/// </summary>
public class ModManagerOptions
{
    [JsonIgnore] public const string Section = "ModManagerOptions";

    /// <summary>The XXMI / 3Dmigoto model-importer root folder (contains d3dx.ini).</summary>
    public string? GimiRootFolderPath { get; set; }

    /// <summary>The Mods root folder that JASM organizes.</summary>
    public string? ModsFolderPath { get; set; }

    public string? UnloadedModsFolderPath { get; set; }

    public bool CharacterSkinsAsCharacters { get; set; }
}
