using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
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
        var result = new List<DepotManifest.FileData>();
        int verified = 0;
        int corrupt = 0;

        foreach (var file in newManifest.Files)
        {
            var decision = GetManifestFileDownloadStatus(
                file,
                oldFiles,
                isUpdate
            );

            if (decision.Download)
                result.Add(file);
            if (decision.Verified)
                verified++;
            if (decision.Corrupt)
                corrupt++;
        }

        if (verified > 0)
            Log($"Verified {verified} existing files");
        if (corrupt > 0)
            Log($"Found {corrupt} corrupt files requiring re-download");

        return result;
    }
}
