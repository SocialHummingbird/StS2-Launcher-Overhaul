using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotFilePlan
    {
        private DepotFilePlan(
            IReadOnlyList<DepotManifest.FileData> downloads,
            IReadOnlyList<string> deletes
        )
        {
            Downloads = downloads;
            Deletes = deletes;
        }

        internal IReadOnlyList<DepotManifest.FileData> Downloads { get; }
        internal IReadOnlyList<string> Deletes { get; }

        internal static DepotFilePlan FromManifestDiff(
            IReadOnlyList<DepotManifest.FileData> downloads,
            IReadOnlyList<string> deletes
        )
            => new(downloads, deletes);
    }

    // Combines manifest changes with on-disk verification so interrupted writes
    // and corrupt files are repaired even without a new manifest.
    private DepotFilePlan BuildDepotFilePlan(
        DepotManifest? oldManifest,
        DepotManifest newManifest,
        bool isUpdate
    )
    {
        var downloads = GetFilesNeedingDownload(oldManifest, newManifest, isUpdate);
        downloads = DeduplicateDownloads(downloads);
        ValidateDownloadFileSizes(downloads);
        return DepotFilePlan.FromManifestDiff(
            downloads,
            GetFilesToDelete(oldManifest, newManifest)
        );
    }
}
