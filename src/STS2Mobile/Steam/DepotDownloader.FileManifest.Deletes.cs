using System;
using System.Collections.Generic;
using System.Linq;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private static List<string> GetFilesToDelete(
        DepotManifest? oldManifest,
        DepotManifest newManifest
    )
    {
        if (oldManifest == null)
            return new List<string>();

        var newFiles = new HashSet<string>(
            NormalizedManifestFileNames(newManifest.Files),
            StringComparer.Ordinal);

        return NormalizedManifestFileNames(oldManifest.Files)
            .Where(f => !newFiles.Contains(f))
            .ToList();
    }
}
