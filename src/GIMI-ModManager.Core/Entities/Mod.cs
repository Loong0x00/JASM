#nullable enable
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using GIMI_ModManager.Core.Contracts.Entities;
using Microsoft.VisualBasic.FileIO;
using SearchOption = System.IO.SearchOption;

namespace GIMI_ModManager.Core.Entities;

// This file was written at the very beginning is just a folder wrapper really
public class Mod : IMod
{
    private protected DirectoryInfo _modDirectory;
    private float? folderSizeInBytes;
    public string FullPath => _modDirectory.FullName;
    public string Name => _modDirectory.Name;
    public string OnlyPath => _modDirectory.Parent!.FullName;

    public Mod(DirectoryInfo modDirectory)
    {
        _modDirectory = modDirectory;
    }

    public bool Exists()
    {
        return _modDirectory.Exists;
    }

    public bool IsEmpty()
    {
        return !_modDirectory.EnumerateFiles().Any();
    }


    public void MoveTo(string absPath)
    {
        if (!Path.IsPathFullyQualified(absPath))
            throw new ArgumentException("Path must be absolute.", nameof(absPath));

        var destination = Path.Combine(absPath, Name);

        // On Windows, different path roots (drive letters) imply a cross-volume move that
        // DirectoryInfo.MoveTo cannot perform, so fall back to copy+delete. On Linux every
        // absolute path shares the "/" root, so GetPathRoot cannot detect a cross-mount move;
        // instead we optimistically try MoveTo and fall back on the IOException it throws when
        // source and destination live on different mount points (EXDEV).
        if (Path.GetPathRoot(absPath) != Path.GetPathRoot(FullPath))
        {
            CopyThenDelete(destination);
            return;
        }

        try
        {
            _modDirectory.MoveTo(destination);
        }
        catch (IOException)
        {
            // Cross-device rename (e.g. mods extracted under /tmp moved to a data disk).
            CopyThenDelete(destination);
        }
    }

    private void CopyThenDelete(string destination)
    {
        var newModDirectory = new DirectoryInfo(destination);
        RecursiveCopyTo(_modDirectory, newModDirectory);
        _modDirectory.Delete(true);
        _modDirectory = newModDirectory;
    }

    public virtual IMod CopyTo(string absPath)
    {
        if (!Path.IsPathFullyQualified(absPath))
            throw new ArgumentException("Path must be absolute.", nameof(absPath));

        var newModDirectory = new DirectoryInfo(Path.Combine(absPath, Name));
        RecursiveCopyTo(_modDirectory, newModDirectory);
        return new Mod(newModDirectory);
    }

    public void Rename(string newName)
    {
        _modDirectory.Refresh();
        if (newName.Equals(_modDirectory.Name, StringComparison.CurrentCultureIgnoreCase))
            return;
        _modDirectory.MoveTo(Path.Combine(OnlyPath, newName));
    }

    public void Delete(bool moveToRecycleBin = true)
    {
        if (moveToRecycleBin)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FileSystem.DeleteDirectory(FullPath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return;
            }

            // Windows' recycle bin API is unavailable off Windows. Use the freedesktop.org
            // trash spec so deletes stay recoverable on Linux; fall back to a permanent
            // delete only if the trash is unusable (e.g. on a different filesystem).
            if (TryMoveToXdgTrash())
                return;
        }

        _modDirectory.Delete(true);
    }

    private bool TryMoveToXdgTrash()
    {
        try
        {
            var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (string.IsNullOrWhiteSpace(dataHome))
                dataHome = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".local", "share");

            var trashDir = Path.Combine(dataHome, "Trash");
            var filesDir = Path.Combine(trashDir, "files");
            var infoDir = Path.Combine(trashDir, "info");
            Directory.CreateDirectory(filesDir);
            Directory.CreateDirectory(infoDir);

            // Pick a name that does not collide with something already in the trash.
            var baseName = Name;
            var targetName = baseName;
            var counter = 1;
            while (Directory.Exists(Path.Combine(filesDir, targetName)) ||
                   File.Exists(Path.Combine(filesDir, targetName)) ||
                   File.Exists(Path.Combine(infoDir, targetName + ".trashinfo")))
            {
                targetName = $"{baseName}.{counter++}";
            }

            var trashInfo = "[Trash Info]\n" +
                            $"Path={EscapeTrashPath(FullPath)}\n" +
                            $"DeletionDate={DateTime.Now:yyyy-MM-ddTHH:mm:ss}\n";
            File.WriteAllText(Path.Combine(infoDir, targetName + ".trashinfo"), trashInfo);

            try
            {
                // A rename only works within the same filesystem; may throw EXDEV otherwise.
                _modDirectory.MoveTo(Path.Combine(filesDir, targetName));
            }
            catch (IOException)
            {
                File.Delete(Path.Combine(infoDir, targetName + ".trashinfo"));
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string EscapeTrashPath(string path)
    {
        // Per the trash spec the Path is percent-encoded but the '/' separators are preserved.
        return string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
    }

    private void RecursiveCopyTo(DirectoryInfo oldModDirectory, DirectoryInfo newModDirectory)
    {
        newModDirectory.Create();
        foreach (var file in oldModDirectory.EnumerateFiles())
            file.CopyTo(Path.Combine(newModDirectory.FullName, file.Name));

        foreach (var directory in oldModDirectory.EnumerateDirectories())
        {
            var newSubDirectory = new DirectoryInfo(Path.Combine(newModDirectory.FullName, directory.Name));
            RecursiveCopyTo(directory, newSubDirectory);
        }
    }

    public bool Equals(IMod? x, IMod? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        return string.Equals(x.FullPath, y.FullPath, StringComparison.CurrentCultureIgnoreCase);
    }

    public bool DeepEquals(IMod? x, IMod? y)
    {
        if (Equals(x, y)) return true;
        if (x is null || y is null) return false;

        var xHash = x.GetContentsHash();
        var yHash = y.GetContentsHash();
        return xHash == yHash;
    }

    // https://stackoverflow.com/a/31349703
    public byte[] GetContentsHash()
    {
        _modDirectory.Refresh();
        var filePaths = Directory.GetFiles(_modDirectory.FullName, "*", SearchOption.AllDirectories).ToArray();
        using var md5 = MD5.Create();
        foreach (var filePath in filePaths)
        {
            // hash path
            var pathBytes = Encoding.UTF8.GetBytes(filePath);
            md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

            // hash contents
            var contentBytes = File.ReadAllBytes(filePath);

            md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
        }

        md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        return md5.Hash ?? Array.Empty<byte>();
    }
    public float GetSizeInGB()
    {
        if (folderSizeInBytes is not null)
            return folderSizeInBytes.Value / 1024f / 1024f / 1024f;
        _modDirectory.Refresh();
        var allFiles = _modDirectory.GetFiles("*", SearchOption.AllDirectories);
        folderSizeInBytes = allFiles.Sum(f => f.Length);
        return folderSizeInBytes.Value / 1024f / 1024f / 1024f;
    }
    public void Refresh()
    {
        folderSizeInBytes = null;
    }

    public int GetHashCode(IMod obj)
    {
        return StringComparer.CurrentCultureIgnoreCase.GetHashCode(obj.FullPath);
    }

    public override string ToString()
    {
        return $"FolderName: {Name} | FullPath: {FullPath}";
    }
}