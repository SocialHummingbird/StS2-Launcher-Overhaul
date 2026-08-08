namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void CleanupStaleDownloadTemps()
    {
        foreach (var temp in EnumerateDownloadingTempFiles())
            TryDeleteFileIfExists(
                temp,
                ex => $"Could not delete stale temp file {temp}: {ex.Message}"
            );
    }
}
