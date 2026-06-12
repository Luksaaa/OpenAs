using Microsoft.Win32;
using OpenAs.Models;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenAs.Services;

public sealed class InstalledFileFormatService
{
    private const int MaxInstalledFormats = 350;
    private const int SFalse = 1;
    private const int AssocFNone = 0;
    private const int AssocStrExecutable = 2;
    private const int AssocStrFriendlyDocName = 3;
    private const int AssocStrFriendlyAppName = 4;

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

        return KnownFileFormats.All
            .Select(AddEffectiveAppToDisplayName)
            .Concat(installedFormats)
            .ToList();
    }

    public FileFormat? Find(string id)
    {
        var knownFormat = KnownFileFormats.Find(id);
        if (knownFormat is not null)
        {
            return AddEffectiveAppToDisplayName(knownFormat);
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

        var extension = "." + extensionName;
        var knownByExtension = KnownFileFormats.All.FirstOrDefault(format =>
            string.Equals(format.Extension, extension, StringComparison.OrdinalIgnoreCase));

        return knownByExtension is null
            ? ReadInstalledFormat(extension)
            : AddEffectiveAppToDisplayName(knownByExtension);
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

        var effectiveExecutable = QueryAssociationString(extension, AssocStrExecutable);
        if (string.IsNullOrWhiteSpace(effectiveExecutable))
        {
            return null;
        }

        var progId = extensionKey.GetValue("") as string;
        if (!string.IsNullOrWhiteSpace(progId) && progId.StartsWith("OpenAs", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var contentType = extensionKey.GetValue("Content Type") as string;
        var perceivedType = extensionKey.GetValue("PerceivedType") as string;
        var friendlyName = QueryAssociationString(extension, AssocStrFriendlyDocName)
            ?? (string.IsNullOrWhiteSpace(progId) ? null : ReadDefaultValue(progId));
        var appName = QueryAssociationString(extension, AssocStrFriendlyAppName);

        return new FileFormat(
            CreateId(extension),
            CreateDisplayName(extension, friendlyName, appName),
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

    private static string? ReadDefaultValue(string subKeyPath)
    {
        using var key = Registry.ClassesRoot.OpenSubKey(subKeyPath);
        return key?.GetValue("") as string;
    }

    private static FileFormat AddEffectiveAppToDisplayName(FileFormat format)
    {
        var appName = QueryAssociationString(format.Extension, AssocStrFriendlyAppName);
        if (string.IsNullOrWhiteSpace(appName))
        {
            return format;
        }

        return format with
        {
            DisplayName = $"{format.DisplayName} ({format.Extension}) - {appName}"
        };
    }

    private static string CreateDisplayName(string extension, string? friendlyName, string? appName)
    {
        var baseName = string.IsNullOrWhiteSpace(friendlyName)
            ? $"{extension.TrimStart('.').ToUpperInvariant()} file"
            : friendlyName;

        if (!string.IsNullOrWhiteSpace(appName))
        {
            return $"{baseName} ({extension}) - {appName}";
        }

        return $"{baseName} ({extension})";
    }

    private static string? QueryAssociationString(string extension, int associationString)
    {
        var length = 0u;
        var result = AssocQueryString(AssocFNone, associationString, extension, null, null, ref length);
        if (result != SFalse || length == 0)
        {
            return null;
        }

        var builder = new StringBuilder((int)length);
        result = AssocQueryString(AssocFNone, associationString, extension, null, builder, ref length);
        if (result < 0)
        {
            return null;
        }

        var value = builder.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string CreateId(string extension) => $"installed:{extension.TrimStart('.').ToLowerInvariant()}";

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int AssocQueryString(
        int flags,
        int str,
        string pszAssoc,
        string? pszExtra,
        StringBuilder? pszOut,
        ref uint pcchOut);
}
