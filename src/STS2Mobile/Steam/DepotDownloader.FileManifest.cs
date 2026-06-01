using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private (
        List<DepotManifest.FileData> Downloads,
        List<string> Deletes
    ) BuildDepotFileLists(
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

        return (Downloads: downloads, Deletes: GetFilesToDelete(oldManifest, newManifest));
    }
}
