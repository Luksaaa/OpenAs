using OpenAs.Models;

namespace OpenAs.Services;

public sealed class FileTypeDetectionService(FileSignatureService signatureService)
{
    public async Task<FileFormat?> DetectAsync(string path)
    {
        foreach (var format in KnownFileFormats.All.Where(CanDetectBySignature))
        {
            if (await signatureService.MatchesAsync(path, format))
            {
                return format;
            }
        }

        return null;
    }

    private static bool CanDetectBySignature(FileFormat format) =>
        format.Signatures.Count > 0
        || string.Equals(format.Id, "mp4", StringComparison.OrdinalIgnoreCase);
}
