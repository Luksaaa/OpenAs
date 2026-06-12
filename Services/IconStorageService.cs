using System.IO;

namespace OpenAs.Services;

public sealed class IconStorageService
{
    public string SaveIconForExtension(string extension, string sourcePath)
    {
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException("Selected icon file does not exist.");
        }

        if (!string.Equals(Path.GetExtension(sourcePath), ".ico", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Choose a .ico file for the custom icon.");
        }

        var iconsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OpenAs",
            "Icons");

        Directory.CreateDirectory(iconsDirectory);

        var targetPath = Path.Combine(iconsDirectory, $"{extension.TrimStart('.')}.ico");
        File.Copy(sourcePath, targetPath, overwrite: true);

        return targetPath;
    }
}
