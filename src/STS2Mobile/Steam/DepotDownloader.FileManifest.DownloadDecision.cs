using System.Collections.Generic;
using System.IO;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private enum ManifestFileDownloadDecision
    {
        Skipped,
        Download,
        Verified,
        Corrupt,
    }

    private ManifestFileDownloadDecision GetManifestFileDownloadDecision(
        DepotManifest.FileData file,
        Dictionary<string, DepotManifest.FileData> oldFiles,
        bool isUpdate
    )
    {
        var fileName = GetDownloadFileName(file);
        if (fileName == null)
            return ManifestFileDownloadDecision.Skipped;

        if (ManifestEntryChanged(file, oldFiles, fileName, isUpdate))
            return ManifestFileDownloadDecision.Download;

        var filePath = ResolveGamePath(fileName);
        if (VerifyFileHash(filePath, file))
        {
            return ManifestFileDownloadDecision.Verified;
        }

        if (File.Exists(filePath))
        {
            Log($"File needs re-download (hash mismatch): {file.FileName}");
            return ManifestFileDownloadDecision.Corrupt;
        }

        return ManifestFileDownloadDecision.Download;
    }

    private string? GetDownloadFileName(DepotManifest.FileData file)
    {
        var fileName = GetManifestFileName(file);
        if (fileName == null)
            return null;

        return file.Flags.HasFlag(EDepotFileFlag.Directory) ? null : fileName;
    }

    private static bool ManifestEntryChanged(
        DepotManifest.FileData file,
        Dictionary<string, DepotManifest.FileData> oldFiles,
        string fileName,
        bool isUpdate
    )
        => isUpdate
            && (
                !oldFiles.TryGetValue(fileName, out var oldFile)
                || !HashesEqual(file.FileHash, oldFile.FileHash)
            );
}
