using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    // Combines manifest changes with on-disk verification so interrupted writes
    // and corrupt files are repaired even without a new manifest.
    private IReadOnlyList<DepotManifest.FileData> BuildDepotDownloads(
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
