using OpenAs.Models;
using System.IO;

namespace OpenAs.Services;

public sealed class FileSignatureService
{
    public async Task<bool> MatchesAsync(string path, FileFormat format)
    {
        if (string.Equals(format.Id, "mp4", StringComparison.OrdinalIgnoreCase))
        {
            return await LooksLikeMp4Async(path);
        }

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

    private static async Task<bool> LooksLikeMp4Async(string path)
    {
        var buffer = new byte[12];

        await using var stream = File.OpenRead(path);
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));

        return read >= 12
            && buffer[4] == (byte)'f'
            && buffer[5] == (byte)'t'
            && buffer[6] == (byte)'y'
            && buffer[7] == (byte)'p';
    }
}
