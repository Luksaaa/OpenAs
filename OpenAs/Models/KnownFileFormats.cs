namespace OpenAs.Models;

public static class KnownFileFormats
{
    public static IReadOnlyList<FileFormat> All { get; } =
    [
        new("jpg", "JPEG image", ".jpg", "image/jpeg", "image", [[0xFF, 0xD8, 0xFF]]),
        new("png", "PNG image", ".png", "image/png", "image", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
        new("gif", "GIF image", ".gif", "image/gif", "image", ["GIF87a"u8.ToArray(), "GIF89a"u8.ToArray()]),
        new("webp", "WebP image", ".webp", "image/webp", "image", ["RIFF"u8.ToArray()]),
        new("mp4", "MP4 video", ".mp4", "video/mp4", "video", []),
        new("pdf", "PDF document", ".pdf", "application/pdf", "document", ["%PDF-"u8.ToArray()]),
        new("zip", "ZIP archive", ".zip", "application/zip", "compressed", [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]]),
        new("txt", "Text file", ".txt", "text/plain", "text", []),
    ];

    public static FileFormat? Find(string id) =>
        All.FirstOrDefault(format => string.Equals(format.Id, id, StringComparison.OrdinalIgnoreCase));
}
