using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotFilePlan
    {
        internal DepotFilePlan(
            List<DepotManifest.FileData> downloads,
            List<string> deletes
        )
        {
            Downloads = downloads;
            Deletes = deletes;
        }

        internal List<DepotManifest.FileData> Downloads { get; }
        internal List<string> Deletes { get; }
    }

    private DepotFilePlan BuildDepotFileLists(
        DepotManifest? oldManifest,
        DepotManifest newManifest,
        bool isUpdate
    )
    {
        // The download list combines manifest changes with on-disk verification so
        // interrupted writes and corrupt files are repaired even without a new manifest.
        var downloads = GetFilesNeedingDownload(oldManifest, newManifest, isUpdate);
        downloads = DeduplicateDownloads(downloads);
        ValidateDownloadFileSizes(downloads);

        return new DepotFilePlan(downloads, GetFilesToDelete(oldManifest, newManifest));
    }
}
