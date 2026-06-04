using System;
using System.Collections.Generic;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void DeleteObsoleteFiles(IEnumerable<string> fileNames)
    {
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

            TryDeleteFileIfExists(
                path,
                $"Deleted: {fileName}",
                ex => $"Could not delete obsolete file {fileName}: {ex.Message}"
            );
        }
    }
}
