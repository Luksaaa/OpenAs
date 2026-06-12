using Microsoft.Win32;
using OpenAs.Models;

namespace OpenAs.Services;

public sealed class InstalledFileFormatService
{
    private const int MaxInstalledFormats = 350;

    public IReadOnlyList<FileFormat> GetAvailableFormats()
    {
        var knownExtensions = KnownFileFormats.All
            .Select(format => format.Extension)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var installedFormats = Registry.ClassesRoot.GetSubKeyNames()
            .Where(IsCandidateExtension)
            .Where(extension => !knownExtensions.Contains(extension))
            .Select(ReadInstalledFormat)
            .OfType<FileFormat>()
            .OrderBy(format => format.Extension, StringComparer.OrdinalIgnoreCase)
            .Take(MaxInstalledFormats)
            .ToList();

        return KnownFileFormats.All.Concat(installedFormats).ToList();
    }

    public FileFormat? Find(string id)
    {
        var knownFormat = KnownFileFormats.Find(id);
        if (knownFormat is not null)
        {
            return knownFormat;
        }

        if (!id.StartsWith("installed:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var extensionName = id["installed:".Length..];
        if (string.IsNullOrWhiteSpace(extensionName))
        {
            return null;
        }

        return ReadInstalledFormat("." + extensionName);
    }

    private static FileFormat? ReadInstalledFormat(string extension)
    {
        if (ExtensionRules.IsBlocked(extension))
        {
            return null;
        }

        using var extensionKey = Registry.ClassesRoot.OpenSubKey(extension);
        if (extensionKey is null)
        {
            return null;
        }

        var progId = extensionKey.GetValue("") as string;
        if (string.IsNullOrWhiteSpace(progId) || progId.StartsWith("OpenAs", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!HasOpenCommand(progId))
        {
            return null;
        }

        var contentType = extensionKey.GetValue("Content Type") as string;
        var perceivedType = extensionKey.GetValue("PerceivedType") as string;
        var friendlyName = ReadDefaultValue(progId);

        return new FileFormat(
            CreateId(extension),
            CreateDisplayName(extension, friendlyName),
            extension,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            string.IsNullOrWhiteSpace(perceivedType) ? "document" : perceivedType,
            []);
    }

    private static bool IsCandidateExtension(string value) =>
        value.StartsWith('.')
        && value.Length is >= 2 and <= 12
        && value.Count(character => character == '.') == 1
        && value.Skip(1).All(character => char.IsAsciiLetterOrDigit(character))
        && !ExtensionRules.IsBlocked(value);

    private static bool HasOpenCommand(string progId)
    {
        using var commandKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
        return !string.IsNullOrWhiteSpace(commandKey?.GetValue("") as string);
    }

    private static string? ReadDefaultValue(string subKeyPath)
    {
        using var key = Registry.ClassesRoot.OpenSubKey(subKeyPath);
        return key?.GetValue("") as string;
    }

    private static string CreateDisplayName(string extension, string? friendlyName)
    {
        if (!string.IsNullOrWhiteSpace(friendlyName))
        {
            return $"{friendlyName} ({extension})";
        }

        return $"{extension.TrimStart('.').ToUpperInvariant()} file ({extension})";
    }

    private static string CreateId(string extension) => $"installed:{extension.TrimStart('.').ToLowerInvariant()}";
}
