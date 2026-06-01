using System;
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
        var nextFileIndex = -1;
        var workerCount = Math.Min(MaxConcurrentDownloads, filesToDownload.Count);
        var workers = Enumerable
            .Range(0, workerCount)
            .Select(
                _ => Task.Run(
                    () => DownloadDepotFileWorkerAsync(
                        TakeNextFile,
                        depotId,
                        depotKey,
                        ct
                    ),
                    ct
                )
            )
            .ToArray();

        await Task.WhenAll(workers);

        DepotManifest.FileData? TakeNextFile()
        {
            var index = Interlocked.Increment(ref nextFileIndex);
            return index < filesToDownload.Count ? filesToDownload[index] : null;
        }
    }

    private async Task DownloadDepotFileWorkerAsync(
        Func<DepotManifest.FileData?> takeNext,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var file = takeNext();
            if (file == null)
                return;

            await DownloadFileAsync(file, depotId, depotKey, ct);
            ForceReportProgress();
        }
    }
}
