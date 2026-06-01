using System.Collections.Generic;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private void ResetDepotProgress(IReadOnlyCollection<DepotManifest.FileData> filesToDownload)
    {
        _progress.TotalBytes = ComputeTotalDownloadBytes(filesToDownload);
        _progress.DownloadedBytes = 0;
        ForceReportProgress();
    }
}
