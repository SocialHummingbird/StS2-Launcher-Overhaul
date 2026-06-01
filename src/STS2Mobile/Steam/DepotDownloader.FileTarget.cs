using System.IO;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotFileTarget
    {
        internal DepotFileTarget(
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

        internal string FileName { get; }
        internal string FilePath { get; }
        internal string? FileDir { get; }
        internal string TempPath { get; }
        internal string LockKey { get; }
    }

    private string? GetManifestFileName(DepotManifest.FileData file)
    {
        var fileName = NormalizeManifestPath(file.FileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Log("Skipping depot file with an empty manifest path");
            return null;
        }

        return fileName;
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
