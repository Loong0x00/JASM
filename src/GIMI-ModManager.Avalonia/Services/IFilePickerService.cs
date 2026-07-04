using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Platform.Storage;

namespace GIMI_ModManager.Avalonia.Services;

public interface IFilePickerService
{
    Task<string?> PickFolderAsync(string? title = null, string? startIn = null);
    Task<string?> PickFileAsync(string? title = null, IReadOnlyList<FilePickerFileType>? filters = null,
        string? startIn = null);
    Task<string?> SaveFileAsync(string? title = null, string? suggestedName = null,
        IReadOnlyList<FilePickerFileType>? filters = null);
}

public class FilePickerService : IFilePickerService
{
    private static TopLevel? TopLevel =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    public async Task<string?> PickFolderAsync(string? title = null, string? startIn = null)
    {
        var top = TopLevel;
        if (top is null)
            return null;

        var options = new FolderPickerOpenOptions
        {
            Title = title ?? "选择文件夹",
            AllowMultiple = false,
            SuggestedStartLocation = await TryGetFolder(top, startIn)
        };

        var result = await top.StorageProvider.OpenFolderPickerAsync(options);
        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> PickFileAsync(string? title = null, IReadOnlyList<FilePickerFileType>? filters = null,
        string? startIn = null)
    {
        var top = TopLevel;
        if (top is null)
            return null;

        var options = new FilePickerOpenOptions
        {
            Title = title ?? "选择文件",
            AllowMultiple = false,
            FileTypeFilter = filters,
            SuggestedStartLocation = await TryGetFolder(top, startIn)
        };

        var result = await top.StorageProvider.OpenFilePickerAsync(options);
        return result.Count > 0 ? result[0].TryGetLocalPath() : null;
    }

    public async Task<string?> SaveFileAsync(string? title = null, string? suggestedName = null,
        IReadOnlyList<FilePickerFileType>? filters = null)
    {
        var top = TopLevel;
        if (top is null)
            return null;

        var options = new FilePickerSaveOptions
        {
            Title = title ?? "保存文件",
            SuggestedFileName = suggestedName,
            FileTypeChoices = filters
        };

        var result = await top.StorageProvider.SaveFilePickerAsync(options);
        return result?.TryGetLocalPath();
    }

    private static async Task<IStorageFolder?> TryGetFolder(TopLevel top, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            return null;
        try
        {
            return await top.StorageProvider.TryGetFolderFromPathAsync(path);
        }
        catch
        {
            return null;
        }
    }
}
