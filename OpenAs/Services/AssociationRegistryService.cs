using Microsoft.Win32;
using OpenAs.Models;

namespace OpenAs.Services;

public sealed class AssociationRegistryService
{
    private const string AppRegistryPath = @"Software\OpenAs\Associations";
    private const string ClassesPath = @"Software\Classes";
    private const string ManagedValueName = "OpenAsManaged";
    private const string PreviousProgIdValueName = "PreviousProgId";
    private const string IconLocationValueName = "IconLocation";
    private readonly FileTypeIconService fileTypeIconService = new();
    private readonly InstalledFileFormatService installedFileFormatService = new();

    public IReadOnlyList<FileAssociation> GetAssociations()
    {
        using var appKey = Registry.CurrentUser.OpenSubKey(AppRegistryPath);
        if (appKey is null)
        {
            return [];
        }

        var associations = new List<FileAssociation>();
        foreach (var extension in appKey.GetSubKeyNames().OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            using var associationKey = appKey.OpenSubKey(extension);
            var formatId = associationKey?.GetValue("Format") as string;
            var progId = associationKey?.GetValue("ProgId") as string;
            var previousProgId = associationKey?.GetValue(PreviousProgIdValueName) as string;
            var iconLocation = associationKey?.GetValue(IconLocationValueName) as string;
            var format = formatId is null ? null : installedFileFormatService.Find(formatId);

            if (format is not null && progId is not null)
            {
                associations.Add(new FileAssociation(extension, format, progId, previousProgId, iconLocation));
            }
        }

        return associations;
    }

    public void SaveAssociation(string extension, FileFormat format, string? customIconPath)
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new InvalidOperationException("Could not resolve OpenAs executable path.");
        }

        var progId = CreateProgId(extension);
        var currentProgId = ReadCurrentProgId(extension);
        if (!string.IsNullOrWhiteSpace(currentProgId) && !string.Equals(currentProgId, progId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{extension} is already registered in Windows. Use a new custom extension to avoid changing existing file behavior.");
        }

        var previousProgId = ReadStoredPreviousProgId(extension);
        var iconLocation = ResolveIconLocation(format, customIconPath);

        using var classesKey = Registry.CurrentUser.CreateSubKey(ClassesPath, true)
            ?? throw new InvalidOperationException("Could not open current-user Classes registry.");

        using (var extensionKey = classesKey.CreateSubKey(extension, true))
        {
            extensionKey.SetValue("", progId);
            extensionKey.SetValue("Content Type", format.MimeType);
            extensionKey.SetValue("PerceivedType", format.PerceivedType);
            extensionKey.SetValue(ManagedValueName, 1, RegistryValueKind.DWord);

            if (!string.IsNullOrWhiteSpace(iconLocation))
            {
                using var extensionIconKey = extensionKey.CreateSubKey("DefaultIcon", true);
                extensionIconKey?.SetValue("", iconLocation);
            }
        }

        using (var progIdKey = classesKey.CreateSubKey(progId, true))
        {
            progIdKey.SetValue("", $"{format.DisplayName} through OpenAs");
            progIdKey.SetValue(ManagedValueName, 1, RegistryValueKind.DWord);

            using var commandKey = progIdKey.CreateSubKey(@"shell\open\command", true);
            commandKey?.SetValue("", $"\"{executablePath}\" --open \"%1\" --format {format.Id}");

            if (!string.IsNullOrWhiteSpace(iconLocation))
            {
                using var iconKey = progIdKey.CreateSubKey("DefaultIcon", true);
                iconKey?.SetValue("", iconLocation);
            }
        }

        using var appKey = Registry.CurrentUser.CreateSubKey($@"{AppRegistryPath}\{extension}", true);
        appKey.SetValue("Format", format.Id);
        appKey.SetValue("ProgId", progId);
        if (!string.IsNullOrWhiteSpace(iconLocation))
        {
            appKey.SetValue(IconLocationValueName, iconLocation);
        }
        else
        {
            appKey.DeleteValue(IconLocationValueName, throwOnMissingValue: false);
        }

        if (!string.IsNullOrWhiteSpace(previousProgId) && !string.Equals(previousProgId, progId, StringComparison.OrdinalIgnoreCase))
        {
            appKey.SetValue(PreviousProgIdValueName, previousProgId);
        }

        ShellNotificationService.NotifyAssociationsChanged();
    }

    public void RemoveAssociation(FileAssociation association)
    {
        using var classesKey = Registry.CurrentUser.OpenSubKey(ClassesPath, true);
        if (classesKey is not null)
        {
            using (var extensionKey = classesKey.OpenSubKey(association.Extension, true))
            {
                if (extensionKey?.GetValue(ManagedValueName) is not null)
                {
                    if (!string.IsNullOrWhiteSpace(association.PreviousProgId))
                    {
                        extensionKey.SetValue("", association.PreviousProgId);
                        extensionKey.DeleteValue(ManagedValueName, throwOnMissingValue: false);
                    }
                    else
                    {
                        classesKey.DeleteSubKeyTree(association.Extension, throwOnMissingSubKey: false);
                    }
                }
            }

            classesKey.DeleteSubKeyTree(association.ProgId, throwOnMissingSubKey: false);
        }

        using var appKey = Registry.CurrentUser.OpenSubKey(AppRegistryPath, true);
        appKey?.DeleteSubKeyTree(association.Extension, throwOnMissingSubKey: false);

        ShellNotificationService.NotifyAssociationsChanged();
    }

    public void RemoveAllAssociations()
    {
        foreach (var association in GetAssociations())
        {
            RemoveAssociation(association);
        }

        ShellNotificationService.NotifyAssociationsChanged();
    }

    private static string? ReadCurrentProgId(string extension)
    {
        using var key = Registry.ClassesRoot.OpenSubKey(extension);
        return key?.GetValue("") as string;
    }

    private static string? ReadStoredPreviousProgId(string extension)
    {
        using var key = Registry.CurrentUser.OpenSubKey($@"{AppRegistryPath}\{extension}");
        return key?.GetValue(PreviousProgIdValueName) as string;
    }

    private static string CreateProgId(string extension) =>
        $"OpenAs{extension.Replace(".", "_", StringComparison.Ordinal)}";

    private string? ResolveIconLocation(FileFormat format, string? customIconPath)
    {
        if (!string.IsNullOrWhiteSpace(customIconPath))
        {
            return $"\"{customIconPath}\"";
        }

        return fileTypeIconService.GetDefaultIconLocation(format.Extension);
    }
}
