using System.IO;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class DepotFileTarget
    {
        private DepotFileTarget(
            string fileName,
            string filePath,
            string? fileDir,
            string tempPath,
            string lockKey
        )
        {
            FileName = fileName;
            FilePath = filePath;
            FileDir = fileDir;
            TempPath = tempPath;
            LockKey = lockKey;
        }

        private string FileName { get; }
        private string FilePath { get; }
        private string? FileDir { get; }
        private string TempPath { get; }
        private string LockKey { get; }
    }

    private bool TryGetManifestFileName(DepotManifest.FileData file, out string fileName)
    {
        fileName = NormalizeManifestPath(file.FileName);
        if (!string.IsNullOrWhiteSpace(fileName))
            return true;

        Log("Skipping depot file with an empty manifest path");
        return false;
    }

    private DepotFileTarget CreateDepotFileTarget(string fileName)
    {
        var filePath = ResolveGamePath(fileName);
        var fileDir = Path.GetDirectoryName(filePath);
        if (fileDir != null)
            Directory.CreateDirectory(fileDir);

        return new DepotFileTarget(
            fileName,
            filePath,
            fileDir,
            filePath + ".downloading",
            Path.GetFullPath(filePath)
        );
    }
}
