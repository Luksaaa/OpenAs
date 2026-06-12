using OpenAs.Models;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace OpenAs.Services;

public sealed class FileOpenService(FileSignatureService signatureService)
{
    public async Task OpenAsync(FileOpenRequest request)
    {
        if (!File.Exists(request.Path))
        {
            MessageBox.Show("The selected file does not exist.", "OpenAs", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!await signatureService.MatchesAsync(request.Path, request.Format))
        {
            MessageBox.Show(
                $"This file does not look like a {request.Format.DisplayName}. OpenAs did not open it.",
                "OpenAs",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var tempPath = CreateTemporaryTypedCopy(request.Path, request.Format.Extension);
        File.Copy(request.Path, tempPath, overwrite: true);

        Process.Start(new ProcessStartInfo
        {
            FileName = tempPath,
            UseShellExecute = true
        });
    }

    private static string CreateTemporaryTypedCopy(string sourcePath, string realExtension)
    {
        var directory = Path.Combine(Path.GetTempPath(), "OpenAs");
        Directory.CreateDirectory(directory);

        var safeName = Path.GetFileNameWithoutExtension(sourcePath);
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidCharacter, '_');
        }

        return Path.Combine(directory, $"{safeName}-{Guid.NewGuid():N}{realExtension}");
    }
}
