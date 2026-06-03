using System.Collections.Generic;
using System.IO;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ManifestFileDownloadStatus
    {
        private ManifestFileDownloadStatus(bool download, bool verified, bool corrupt)
        {
            Download = download;
            Verified = verified;
            Corrupt = corrupt;
        }

        internal bool Download { get; }
        internal bool Verified { get; }
        internal bool Corrupt { get; }

        internal static ManifestFileDownloadStatus Skip()
            => new(false, false, false);

        internal static ManifestFileDownloadStatus NeedsDownload()
            => new(true, false, false);

        internal static ManifestFileDownloadStatus ExistingVerified()
            => new(false, true, false);

        internal static ManifestFileDownloadStatus CorruptNeedsDownload()
            => new(true, false, true);
    }

    private ManifestFileDownloadStatus GetManifestFileDownloadStatus(
        DepotManifest.FileData file,
        Dictionary<string, DepotManifest.FileData> oldFiles,
        bool isUpdate
    )
    {
        var fileName = GetDownloadFileName(file);
        if (fileName == null)
            return ManifestFileDownloadStatus.Skip();

        if (ManifestEntryChanged(file, oldFiles, fileName, isUpdate))
            return ManifestFileDownloadStatus.NeedsDownload();

        var filePath = ResolveGamePath(fileName);
        if (VerifyFileHash(filePath, file))
            return ManifestFileDownloadStatus.ExistingVerified();

        if (File.Exists(filePath))
        {
            Log($"File needs re-download (hash mismatch): {file.FileName}");
            return ManifestFileDownloadStatus.CorruptNeedsDownload();
        }

        return ManifestFileDownloadStatus.NeedsDownload();
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
