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

    private readonly struct ManifestFileDownloadStatus
    {
        private ManifestFileDownloadStatus(ManifestFileDownloadState state)
        {
            State = state;
        }

        private ManifestFileDownloadState State { get; }
        internal bool Download
            => State == ManifestFileDownloadState.NeedsDownload
                || State == ManifestFileDownloadState.CorruptNeedsDownload;
        internal bool Verified => State == ManifestFileDownloadState.ExistingVerified;
        internal bool Corrupt => State == ManifestFileDownloadState.CorruptNeedsDownload;

        internal static ManifestFileDownloadStatus Skip()
            => new(ManifestFileDownloadState.Skip);

        internal static ManifestFileDownloadStatus NeedsDownload()
            => new(ManifestFileDownloadState.NeedsDownload);

        internal static ManifestFileDownloadStatus ExistingVerified()
            => new(ManifestFileDownloadState.ExistingVerified);

        internal static ManifestFileDownloadStatus CorruptNeedsDownload()
            => new(ManifestFileDownloadState.CorruptNeedsDownload);
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
