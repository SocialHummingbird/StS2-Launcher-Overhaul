using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task DownloadDepotFilesAsync(
        IReadOnlyList<DepotManifest.FileData> filesToDownload,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        var workerCount = Math.Min(MaxConcurrentDownloads, filesToDownload.Count);
        var nextFileIndex = -1;
        var workers = Enumerable
            .Range(0, workerCount)
            .Select(
                _ =>
                    Task.Run(
                        async () =>
                        {
                            while (true)
                            {
                                ct.ThrowIfCancellationRequested();
                                var index = Interlocked.Increment(ref nextFileIndex);
                                if (index >= filesToDownload.Count)
                                    break;

                                await DownloadFileAsync(
                                    filesToDownload[index],
                                    depotId,
                                    depotKey,
                                    ct
                                );
                                ForceReportProgress();
                            }
                        },
                        ct
                    )
            )
            .ToArray();

        await Task.WhenAll(workers);
    }
}
