using System;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed partial class DownloadStateStore
    {
        private void CleanupTempFiles()
        {
            try
            {
                foreach (var path in Directory.GetFiles(_stateDir, "*.tmp"))
                    DeleteQuietly(path);
            }
            catch (Exception ex)
            {
                Log($"Could not clean download state temp files: {ex.Message}");
            }
        }

        private void MoveBadStateFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return;

                var badPath = path + ".bad";
                if (File.Exists(badPath))
                    File.Delete(badPath);

                File.Move(path, badPath);
            }
            catch (Exception ex)
            {
                Log($"Could not quarantine bad state file {Path.GetFileName(path)}: {ex.Message}");
            }
        }

        private static void CommitStateFile(string tempPath, string targetPath)
        {
            try
            {
                File.Move(tempPath, targetPath, overwrite: true);
            }
            catch (Exception ex)
            {
                DeleteQuietly(tempPath);
                throw new IOException(
                    $"Failed to commit downloader state file {Path.GetFileName(targetPath)}: {ex.Message}",
                    ex
                );
            }
        }
    }
}
