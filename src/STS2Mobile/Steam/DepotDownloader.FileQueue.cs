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

    private sealed class DepotFileDownloadWorkers
    {
        private readonly DepotDownloader _owner;
        private readonly DepotFileDownloadQueue _queue;
        private readonly uint _depotId;
        private readonly byte[] _depotKey;
        private readonly CancellationToken _ct;

        internal DepotFileDownloadWorkers(
            DepotDownloader owner,
            DepotFileDownloadQueue queue,
            uint depotId,
            byte[] depotKey,
            CancellationToken ct
        )
        {
            _owner = owner;
            _queue = queue;
            _depotId = depotId;
            _depotKey = depotKey;
            _ct = ct;
        }

        internal Task RunAsync()
        {
            var workers = Enumerable
                .Range(0, _queue.WorkerCount)
                .Select(_ => Task.Run(RunWorkerAsync, _ct))
                .ToArray();

            return Task.WhenAll(workers);
        }

        private async Task RunWorkerAsync()
        {
            while (true)
            {
                _ct.ThrowIfCancellationRequested();
                var file = _queue.TakeNext();
                if (file == null)
                    return;

                await _owner.DownloadFileAsync(file, _depotId, _depotKey, _ct);
                _owner.ForceReportProgress();
            }
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
        var workers = new DepotFileDownloadWorkers(this, queue, depotId, depotKey, ct);
        await workers.RunAsync();
    }
}
