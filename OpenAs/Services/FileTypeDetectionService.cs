using OpenAs.Models;

namespace OpenAs.Services;

public sealed class FileTypeDetectionService(FileSignatureService signatureService)
{
    public async Task<FileFormat?> DetectAsync(string path)
    {
        foreach (var format in KnownFileFormats.All.Where(format => format.Signatures.Count > 0))
        {
            if (await signatureService.MatchesAsync(path, format))
            {
                return format;
            }
        }

        return null;
    }
}
