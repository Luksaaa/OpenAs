using Microsoft.Win32;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenAs.Services;

public sealed class FileTypeIconService
{
    private const int SFalse = 1;
    private const int AssocFNone = 0;
    private const int AssocStrDefaultIcon = 15;

    public string? GetDefaultIconLocation(string extension)
    {
        var effectiveIcon = QueryEffectiveDefaultIcon(extension);
        if (!string.IsNullOrWhiteSpace(effectiveIcon))
        {
            return effectiveIcon;
        }

        var extensionIcon = ReadDefaultValue($@"{extension}\DefaultIcon");
        if (!string.IsNullOrWhiteSpace(extensionIcon))
        {
            return extensionIcon;
        }

        var progId = ReadDefaultValue(extension);
        if (string.IsNullOrWhiteSpace(progId))
        {
            return null;
        }

        return ReadDefaultValue($@"{progId}\DefaultIcon");
    }

    private static string? QueryEffectiveDefaultIcon(string extension)
    {
        var length = 0u;
        var result = AssocQueryString(AssocFNone, AssocStrDefaultIcon, extension, null, null, ref length);
        if (result != SFalse || length == 0)
        {
            return null;
        }

        var builder = new StringBuilder((int)length);
        result = AssocQueryString(AssocFNone, AssocStrDefaultIcon, extension, null, builder, ref length);
        if (result < 0)
        {
            throw new Win32Exception(result);
        }

        return builder.ToString();
    }

    private static string? ReadDefaultValue(string subKeyPath)
    {
        using var key = Registry.ClassesRoot.OpenSubKey(subKeyPath);
        return key?.GetValue("") as string;
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int AssocQueryString(
        int flags,
        int str,
        string pszAssoc,
        string? pszExtra,
        StringBuilder? pszOut,
        ref uint pcchOut);
}
