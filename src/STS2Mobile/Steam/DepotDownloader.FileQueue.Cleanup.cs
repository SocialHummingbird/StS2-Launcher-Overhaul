using System;
using System.IO;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void CleanupStaleDownloadTemps()
    {
        foreach (var temp in EnumerateDownloadingTempFiles())
        {
            try
            {
                File.Delete(temp);
            }
            catch (Exception ex)
            {
                Log($"Could not delete stale temp file {temp}: {ex.Message}");
            }
        }
    }
}
