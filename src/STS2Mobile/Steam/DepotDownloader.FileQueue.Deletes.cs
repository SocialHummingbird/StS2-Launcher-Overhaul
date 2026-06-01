using System;
using System.Collections.Generic;
using System.IO;

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

            if (!File.Exists(path))
                continue;

            try
            {
                File.Delete(path);
                Log($"Deleted: {fileName}");
            }
            catch (Exception ex)
            {
                Log($"Could not delete obsolete file {fileName}: {ex.Message}");
            }
        }
    }
}
