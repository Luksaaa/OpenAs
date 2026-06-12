using OpenAs.Models;
using System.IO;

namespace OpenAs.Services;

public sealed class FileSignatureService
{
    public async Task<bool> MatchesAsync(string path, FileFormat format)
    {
        if (format.Signatures.Count == 0)
        {
            return true;
        }

        var maxLength = format.Signatures.Max(signature => signature.Length);
        var buffer = new byte[maxLength];

        await using var stream = File.OpenRead(path);
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

        return format.Signatures.Any(signature =>
            read >= signature.Length && buffer.AsSpan(0, signature.Length).SequenceEqual(signature));
    }
}
