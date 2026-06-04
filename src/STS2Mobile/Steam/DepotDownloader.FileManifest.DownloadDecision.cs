using System.Collections.Generic;
using System.IO;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private enum ManifestFileDownloadState
    {
        Skip,
        NeedsDownload,
        ExistingVerified,
        CorruptNeedsDownload,
    }

    private ManifestFileDownloadState GetManifestFileDownloadState(
        DepotManifest.FileData file,
        Dictionary<string, DepotManifest.FileData> oldFiles,
        bool isUpdate
    )
    {
        var fileName = GetDownloadFileName(file);
        if (fileName == null)
            return ManifestFileDownloadState.Skip;

        if (ManifestEntryChanged(file, oldFiles, fileName, isUpdate))
            return ManifestFileDownloadState.NeedsDownload;

        var filePath = ResolveGamePath(fileName);
        if (VerifyFileHash(filePath, file))
            return ManifestFileDownloadState.ExistingVerified;

        if (File.Exists(filePath))
        {
            Log($"File needs re-download (hash mismatch): {file.FileName}");
            return ManifestFileDownloadState.CorruptNeedsDownload;
        }

        return ManifestFileDownloadState.NeedsDownload;
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
