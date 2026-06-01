using System.Collections.Generic;
using System.IO;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ManifestFileDownloadDecision
    {
        internal ManifestFileDownloadDecision(
            bool shouldDownload,
            bool verifiedExisting,
            bool corruptExisting
        )
        {
            ShouldDownload = shouldDownload;
            VerifiedExisting = verifiedExisting;
            CorruptExisting = corruptExisting;
        }

        internal bool ShouldDownload { get; }
        internal bool VerifiedExisting { get; }
        internal bool CorruptExisting { get; }

        internal static ManifestFileDownloadDecision Skip =>
            new(shouldDownload: false, verifiedExisting: false, corruptExisting: false);

        internal static ManifestFileDownloadDecision Download =>
            new(shouldDownload: true, verifiedExisting: false, corruptExisting: false);

        internal static ManifestFileDownloadDecision Verified =>
            new(shouldDownload: false, verifiedExisting: true, corruptExisting: false);

        internal static ManifestFileDownloadDecision Corrupt =>
            new(shouldDownload: true, verifiedExisting: false, corruptExisting: true);
    }

    private ManifestFileDownloadDecision GetManifestFileDownloadDecision(
        DepotManifest.FileData file,
        Dictionary<string, DepotManifest.FileData> oldFiles,
        bool isUpdate
    )
    {
        if (!TryGetDownloadFileName(file, out var fileName))
            return ManifestFileDownloadDecision.Skip;

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

    private bool TryGetDownloadFileName(DepotManifest.FileData file, out string fileName)
    {
        if (!TryGetManifestFileName(file, out fileName))
            return false;

        return !file.Flags.HasFlag(EDepotFileFlag.Directory);
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
