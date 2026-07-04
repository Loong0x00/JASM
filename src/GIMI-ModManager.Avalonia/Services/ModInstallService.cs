using GIMI_ModManager.Core.Contracts.Entities;
using GIMI_ModManager.Core.Contracts.Services;
using GIMI_ModManager.Core.Entities.Mods.SkinMod;
using GIMI_ModManager.Core.Services;
using Serilog;

namespace GIMI_ModManager.Avalonia.Services;

/// <summary>
/// Installs a mod from a folder or an archive (.zip/.rar/.7z, extracted via SharpCompress) into a
/// character's mod list. Handles finding the actual mod root inside a wrapping archive folder.
/// </summary>
public class ModInstallService
{
    private readonly ISkinManagerService _skinManagerService;
    private readonly ArchiveService _archiveService;
    private readonly ILogger _logger;

    private static readonly string InstallTmpRoot = Path.Combine(Path.GetTempPath(), "JASM_install");

    public ModInstallService(ISkinManagerService skinManagerService, ArchiveService archiveService, ILogger logger)
    {
        _skinManagerService = skinManagerService;
        _archiveService = archiveService;
        _logger = logger.ForContext<ModInstallService>();
    }

    public static bool IsSupportedArchive(string path) => ArchiveService.IsArchive(path);

    /// <summary>Extracts an archive to a temp folder and returns the detected mod root.</summary>
    public DirectoryInfo ExtractArchive(string archivePath)
    {
        Directory.CreateDirectory(InstallTmpRoot);
        var dest = Path.Combine(InstallTmpRoot, Guid.NewGuid().ToString("N"));
        var extracted = _archiveService.ExtractArchive(archivePath, dest);
        return FindModRoot(extracted);
    }

    /// <summary>Creates the mod and adds it to the target character (copying by default).</summary>
    public async Task InstallAsync(string modFolder, ICharacterModList targetList, bool move = false)
    {
        var mod = await SkinMod.CreateModAsync(modFolder);
        using (targetList.DisableWatcher())
        {
            _skinManagerService.AddMod(mod, targetList, move);
        }
    }

    /// <summary>
    /// Descends through wrapper folders (a single subfolder with no files) to find the folder that
    /// actually contains the mod's .ini files.
    /// </summary>
    private static DirectoryInfo FindModRoot(DirectoryInfo start)
    {
        var current = start;
        for (var depth = 0; depth < 8; depth++)
        {
            var hasIni = current.EnumerateFiles("*.ini", SearchOption.TopDirectoryOnly).Any();
            if (hasIni)
                return current;

            var subDirs = current.GetDirectories();
            var files = current.GetFiles();

            // Pure wrapper folder → descend into the single child.
            if (subDirs.Length == 1 && files.Length == 0)
            {
                current = subDirs[0];
                continue;
            }

            break;
        }

        return current;
    }

    public static void CleanupTemp()
    {
        try
        {
            if (Directory.Exists(InstallTmpRoot))
                Directory.Delete(InstallTmpRoot, true);
        }
        catch
        {
            // best-effort
        }
    }
}
