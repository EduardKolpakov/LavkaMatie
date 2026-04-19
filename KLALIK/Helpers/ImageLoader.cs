using System.IO;
using Avalonia.Media.Imaging;

namespace KLALIK.Helpers;

public static class ImageLoader
{
    public static Bitmap? TryLoadFromBaseDirectory(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;
        var full = Path.Combine(AppContext.BaseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full))
            return null;
        try
        {
            return new Bitmap(full);
        }
        catch
        {
            return null;
        }
    }
}
