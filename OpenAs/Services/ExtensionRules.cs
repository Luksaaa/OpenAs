namespace OpenAs.Services;

public static class ExtensionRules
{
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".com", ".bat", ".cmd", ".ps1", ".psm1", ".vbs", ".vbe", ".js", ".jse",
        ".wsf", ".wsh", ".msi", ".msp", ".scr", ".pif", ".lnk", ".reg", ".scf", ".cpl",
        ".dll", ".sys", ".drv"
    };

    public static bool IsBlocked(string extension) => BlockedExtensions.Contains(extension);

    public static string Normalize(string value)
    {
        var extension = value.Trim();
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException("Enter a custom extension first.");
        }

        if (!extension.StartsWith('.'))
        {
            extension = "." + extension;
        }

        extension = extension.ToLowerInvariant();

        if (extension.Length < 2 || extension.Length > 32)
        {
            throw new InvalidOperationException("Extension must be between 1 and 31 characters after the dot.");
        }

        if (extension.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '_' and not '-'))
        {
            throw new InvalidOperationException("Use only letters, numbers, dash, underscore, and dot.");
        }

        if (extension.Count(character => character == '.') != 1)
        {
            throw new InvalidOperationException("Use a single extension like .finger.");
        }

        if (BlockedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"{extension} is blocked because Windows treats it as executable or system-sensitive.");
        }

        return extension;
    }
}
