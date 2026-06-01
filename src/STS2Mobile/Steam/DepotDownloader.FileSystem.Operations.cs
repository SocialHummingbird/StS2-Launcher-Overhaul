using System;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void CommitDownloadedFile(string tempPath, string filePath, string fileName)
    {
        try
        {
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            throw new IOException($"Failed to commit downloaded file {fileName}: {ex.Message}", ex);
        }
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Depot] Failed to delete temporary file {path}: {ex.Message}");
        }
    }
}
