using System.Collections.Generic;
using System.Linq;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private List<DepotManifest.FileData> DeduplicateDownloads(
        List<DepotManifest.FileData> filesToDownload
    )
    {
        if (filesToDownload.Count <= 1)
            return filesToDownload;

        var deduped = BuildManifestFileMap(filesToDownload);

        if (deduped.Count == filesToDownload.Count)
            return filesToDownload;

        Log(
            $"Deduplicated duplicate download queue entries: {filesToDownload.Count - deduped.Count}"
        );
        return deduped.Values.ToList();
    }
}
