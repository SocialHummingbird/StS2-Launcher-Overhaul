using System;
using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static Dictionary<string, DepotManifest.FileData> BuildManifestFileMap(
        DepotManifest? manifest
    )
        => manifest == null
            ? new Dictionary<string, DepotManifest.FileData>(StringComparer.Ordinal)
            : BuildManifestFileMap(manifest.Files);

    private static Dictionary<string, DepotManifest.FileData> BuildManifestFileMap(
        IEnumerable<DepotManifest.FileData> files
    )
    {
        var map = new Dictionary<string, DepotManifest.FileData>(StringComparer.Ordinal);

        foreach (var file in files)
        {
            if (!TryGetNormalizedManifestFileName(file, out var fileName))
                continue;

            map[fileName] = file;
        }

        return map;
    }

    private static IEnumerable<string> NormalizedManifestFileNames(
        IEnumerable<DepotManifest.FileData> files
    )
    {
        foreach (var file in files)
        {
            if (TryGetNormalizedManifestFileName(file, out var fileName))
                yield return fileName;
        }
    }

    private static bool TryGetNormalizedManifestFileName(
        DepotManifest.FileData file,
        out string fileName
    )
    {
        fileName = NormalizeManifestPath(file.FileName);
        return !string.IsNullOrEmpty(fileName);
    }
}
