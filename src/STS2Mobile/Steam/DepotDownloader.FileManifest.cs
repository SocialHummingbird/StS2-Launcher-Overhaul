using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct DepotFilePlan
    {
        private DepotFilePlan(
            List<DepotManifest.FileData> downloads,
            List<string> deletes
        )
        {
            Downloads = downloads;
            Deletes = deletes;
        }

        private List<DepotManifest.FileData> Downloads { get; }
        private List<string> Deletes { get; }

        internal static DepotFilePlan Create(
            List<DepotManifest.FileData> downloads,
            List<string> deletes
        )
            => new(downloads, deletes);

        internal void ApplyDeletes(DepotDownloader owner)
            => owner.DeleteObsoleteFiles(Deletes);

        internal void ResetProgress(DepotDownloader owner)
            => owner.ResetDepotProgress(Downloads);

        internal async Task DownloadAsync(
            DepotDownloader owner,
            uint depotId,
            byte[] depotKey,
            CancellationToken ct
        )
        {
            if (Downloads.Count == 0)
            {
                owner.Log($"Depot {depotId}: already up to date");
                return;
            }

            owner.Log(
                $"Downloading {Downloads.Count} files ({FormatBytes(owner._totalDownloadBytes)}) with {MaxConcurrentDownloads} threads..."
            );

            await owner.DownloadDepotFilesAsync(Downloads, depotId, depotKey, ct);
        }
    }

    private DepotFilePlan BuildDepotFileLists(
        DepotManifest? oldManifest,
        DepotManifest newManifest,
        bool isUpdate
    )
    {
        // The download list combines manifest changes with on-disk verification so
        // interrupted writes and corrupt files are repaired even without a new manifest.
        return DepotFilePlan.Create(
            BuildValidatedDownloadList(oldManifest, newManifest, isUpdate),
            GetFilesToDelete(oldManifest, newManifest)
        );
    }

    private List<DepotManifest.FileData> BuildValidatedDownloadList(
        DepotManifest? oldManifest,
        DepotManifest newManifest,
        bool isUpdate
    )
    {
        var downloads = GetFilesNeedingDownload(oldManifest, newManifest, isUpdate);
        downloads = DeduplicateDownloads(downloads);
        ValidateDownloadFileSizes(downloads);
        return downloads;
    }
}
