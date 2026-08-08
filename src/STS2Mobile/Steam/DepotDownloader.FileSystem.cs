using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private IEnumerable<string> EnumerateDownloadingTempFiles()
    {
        try
        {
            return Directory.GetFiles(_gameDir, "*.downloading", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            Log($"Could not enumerate stale temp downloads: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    private string ResolveGamePath(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new IOException("Depot manifest contained an empty file path");
        }

        var normalized = NormalizeManifestPath(manifestPath)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(_gameDir);
        var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? root
            : root + Path.DirectorySeparatorChar;

        if (
            !string.Equals(fullPath, root, StringComparison.Ordinal)
            && !fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal)
        )
        {
            throw new IOException($"Depot path escapes game directory: {manifestPath}");
        }

        return fullPath;
    }

    private static string NormalizeManifestPath(string manifestPath)
    {
        if (manifestPath == null)
        {
            return string.Empty;
        }

        var nullIndex = manifestPath.IndexOf('\0');
        if (nullIndex >= 0)
        {
            manifestPath = manifestPath[..nullIndex];
        }

        return manifestPath.Replace('\\', '/');
    }
}
