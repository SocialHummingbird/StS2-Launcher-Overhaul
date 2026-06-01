using System;
using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static Dictionary<string, DepotManifest.FileData> BuildManifestFileMap(
        DepotManifest? manifest
    )
    {
        var files = new Dictionary<string, DepotManifest.FileData>(StringComparer.Ordinal);

        if (manifest == null)
            return files;

        foreach (var file in manifest.Files)
        {
            var fileName = NormalizeManifestPath(file.FileName);
            if (string.IsNullOrEmpty(fileName))
                continue;

            files[fileName] = file;
        }

        return files;
    }
}
