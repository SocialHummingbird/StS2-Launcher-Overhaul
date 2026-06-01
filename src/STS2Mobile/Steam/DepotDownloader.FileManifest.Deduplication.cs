using System;
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

        var deduped = new Dictionary<string, DepotManifest.FileData>(StringComparer.Ordinal);
        foreach (var file in filesToDownload)
        {
            var fileName = NormalizeManifestPath(file.FileName);
            if (!string.IsNullOrEmpty(fileName))
                deduped[fileName] = file;
        }

        if (deduped.Count == filesToDownload.Count)
            return filesToDownload;

        Log(
            $"Deduplicated duplicate download queue entries: {filesToDownload.Count - deduped.Count}"
        );
        return deduped.Values.ToList();
    }
}
