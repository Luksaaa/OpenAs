namespace OpenAs.Models;

public sealed record FileFormat(
    string Id,
    string DisplayName,
    string Extension,
    string MimeType,
    string PerceivedType,
    IReadOnlyList<byte[]> Signatures);
