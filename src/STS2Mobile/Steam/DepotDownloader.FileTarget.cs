using System.IO;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
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

    private (
        string FileName,
        string FilePath,
        string? FileDir,
        string TempPath,
        string LockKey
    ) CreateDepotFilePaths(string fileName)
    {
        var filePath = ResolveGamePath(fileName);
        var fileDir = Path.GetDirectoryName(filePath);
        if (fileDir != null)
            Directory.CreateDirectory(fileDir);

        return (
            FileName: fileName,
            FilePath: filePath,
            FileDir: fileDir,
            TempPath: filePath + ".downloading",
            LockKey: Path.GetFullPath(filePath)
        );
    }
}
