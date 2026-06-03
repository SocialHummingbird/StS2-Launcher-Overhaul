using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private sealed class DepotFileDownloadQueue
    {
        private readonly IReadOnlyList<DepotManifest.FileData> _files;
        private int _nextFileIndex = -1;

        internal DepotFileDownloadQueue(IReadOnlyList<DepotManifest.FileData> files)
        {
            _files = files;
        }

        internal int WorkerCount
            => Math.Min(MaxConcurrentDownloads, _files.Count);

        internal DepotManifest.FileData? TakeNext()
        {
            var index = Interlocked.Increment(ref _nextFileIndex);
            return index < _files.Count ? _files[index] : null;
        }
    }

    private async Task DownloadDepotFilesAsync(
        IReadOnlyList<DepotManifest.FileData> filesToDownload,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        var queue = new DepotFileDownloadQueue(filesToDownload);
        var workers = Enumerable
            .Range(0, queue.WorkerCount)
            .Select(
                _ => Task.Run(
                    () => DownloadDepotFileWorkerAsync(
                        queue,
                        depotId,
                        depotKey,
                        ct
                    ),
                    ct
                )
            )
            .ToArray();

        await Task.WhenAll(workers);
    }

    private async Task DownloadDepotFileWorkerAsync(
        DepotFileDownloadQueue queue,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var file = queue.TakeNext();
            if (file == null)
                return;

            await DownloadFileAsync(file, depotId, depotKey, ct);
            ForceReportProgress();
        }
    }
}
