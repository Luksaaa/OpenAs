namespace OpenAs.Models;

public sealed record FileOpenRequest(string Path, FileFormat Format)
{
    public static bool TryParse(string[] args, out FileOpenRequest request)
    {
        request = default!;

        if (args.Length < 4 || !string.Equals(args[0], "--open", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var path = args[1];
        var formatIndex = Array.FindIndex(args, value => string.Equals(value, "--format", StringComparison.OrdinalIgnoreCase));
        if (formatIndex < 0 || formatIndex + 1 >= args.Length)
        {
            return false;
        }

        var format = KnownFileFormats.Find(args[formatIndex + 1]);
        if (format is null)
        {
            return false;
        }

        request = new FileOpenRequest(path, format);
        return true;
    }
}
