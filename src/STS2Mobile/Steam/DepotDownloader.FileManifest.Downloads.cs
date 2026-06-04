using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class ManifestDownloadPlanBuilder
    {
        private readonly List<DepotManifest.FileData> _downloads = new();
        private int _corrupt;
        private int _verified;

        internal void Add(DepotManifest.FileData file, ManifestFileDownloadStatus status)
        {
            if (status.Download)
                _downloads.Add(file);
            if (status.Verified)
                _verified++;
            if (status.Corrupt)
                _corrupt++;
        }

        internal List<DepotManifest.FileData> Downloads()
            => _downloads;

        internal void LogSummary(DepotDownloader owner)
        {
            if (_verified > 0)
                owner.Log($"Verified {_verified} existing files");
            if (_corrupt > 0)
                owner.Log($"Found {_corrupt} corrupt files requiring re-download");
        }
    }

    // Builds the list of files that need downloading. For manifest changes, uses
    // the hash diff. For all files in the target manifest, verifies the on-disk
    // copy against the expected SHA-1, catching corruption from interrupted
    // writes, disk errors, or missing files.
    private List<DepotManifest.FileData> GetFilesNeedingDownload(
        DepotManifest? oldManifest,
        DepotManifest newManifest,
        bool isUpdate
    )
    {
        var oldFiles = BuildManifestFileMap(oldManifest);
        var plan = new ManifestDownloadPlanBuilder();

        foreach (var file in newManifest.Files)
        {
            var decision = GetManifestFileDownloadStatus(
                file,
                oldFiles,
                isUpdate
            );

            plan.Add(file, decision);
        }

        plan.LogSummary(this);
        return plan.Downloads();
    }
}
