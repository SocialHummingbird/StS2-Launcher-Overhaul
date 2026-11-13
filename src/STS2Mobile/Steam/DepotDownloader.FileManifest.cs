using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SteamKit2;

namespace STS2Mobile.Steam;
internal sealed partial class DepotDownloader
{
    private static List<string> GetFilesToDelete(DepotManifest? oldManifest, DepotManifest newManifest)
    {
        if (oldManifest == null)
            return new List<string>();
        var newFiles = new HashSet<string>(NormalizedManifestFileNames(newManifest.Files), StringComparer.Ordinal);
        return NormalizedManifestFileNames(oldManifest.Files).Where(f => !newFiles.Contains(f)).ToList();
    }

    private static IEnumerable<string> NormalizedManifestFileNames(IEnumerable<DepotManifest.FileData> files)
    {
        foreach (var file in files)
        {
            if (TryGetNormalizedManifestFileName(file, out var fileName))
                yield return fileName;
        }
    }

    private static bool TryGetNormalizedManifestFileName(DepotManifest.FileData file, out string fileName)
    {
        fileName = NormalizeManifestPath(file.FileName);
        return !string.IsNullOrEmpty(fileName);
    }

    private void DeleteObsoleteFiles(IEnumerable<string> fileNames)
    {
        RequireUpdatingBeforeInstalledMutation();
        foreach (var fileName in fileNames)
        {
            string path;
            try
            {
                path = ResolveGamePath(fileName);
            }
            catch (Exception ex)
            {
                Log($"Skipping obsolete file with invalid cached path {fileName}: {ex.Message}");
                continue;
            }

            TryDeleteFileIfExists(path, $"Deleted: {fileName}", ex => $"Could not delete obsolete file {fileName}: {ex.Message}");
        }
    }

    private static void ValidateDownloadFileSizes(IEnumerable<DepotManifest.FileData> files)
    {
        foreach (var file in files)
        {
            if (file.TotalSize > (ulong)MaxDepotFileBytes)
            {
                throw new IOException($"Depot file is unexpectedly large for {file.FileName}: " + $"{file.TotalSize} bytes");
            }
        }
    }

    private static long ComputeTotalDownloadBytes(IEnumerable<DepotManifest.FileData> files)
    {
        long total = 0;
        foreach (var file in files)
        {
            try
            {
                total = checked(total + (long)file.TotalSize);
            }
            catch (OverflowException ex)
            {
                throw new IOException($"Depot download size is too large while adding {file.FileName}: " + $"{file.TotalSize} bytes", ex);
            }
        }

        return total;
    }
}
