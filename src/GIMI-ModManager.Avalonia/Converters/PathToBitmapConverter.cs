using System.Collections.Concurrent;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace GIMI_ModManager.Avalonia.Converters;

/// <summary>
/// Converts an absolute file path (string) into a cached <see cref="Bitmap"/> for Image.Source.
/// Returns null for missing files so the UI can fall back to a placeholder.
/// </summary>
public class PathToBitmapConverter : IValueConverter
{
    public static readonly PathToBitmapConverter Instance = new();

    private static readonly ConcurrentDictionary<string, Bitmap?> Cache = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return null;

        return Cache.GetOrAdd(path, static p =>
        {
            try
            {
                return File.Exists(p) ? new Bitmap(p) : null;
            }
            catch
            {
                return null;
            }
        });
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
