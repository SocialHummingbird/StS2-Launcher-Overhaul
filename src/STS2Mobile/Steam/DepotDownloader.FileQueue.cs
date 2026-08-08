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

    private readonly struct DepotFileDownloadContext
    {
        private DepotFileDownloadContext(
            DepotDownloader owner,
            uint depotId,
            byte[] depotKey,
            CancellationToken ct
        )
        {
            Owner = owner;
            DepotId = depotId;
            DepotKey = depotKey;
            Cancellation = ct;
        }

        internal CancellationToken Cancellation { get; }

        private DepotDownloader Owner { get; }
        private uint DepotId { get; }
        private byte[] DepotKey { get; }

        internal async Task DownloadAsync(DepotManifest.FileData file)
        {
            await Owner.DownloadFileAsync(file, DepotId, DepotKey, Cancellation);
            Owner.ForceReportProgress();
        }

        internal static DepotFileDownloadContext Create(
            DepotDownloader owner,
            uint depotId,
            byte[] depotKey,
            CancellationToken ct
        )
            => new(owner, depotId, depotKey, ct);
    }

    private sealed class DepotFileDownloadWorkers
    {
        private readonly DepotFileDownloadQueue _queue;
        private readonly DepotFileDownloadContext _context;

        private DepotFileDownloadWorkers(
            DepotFileDownloadQueue queue,
            DepotFileDownloadContext context
        )
        {
            _queue = queue;
            _context = context;
        }

        private Task RunAsync()
        {
            var workers = Enumerable
                .Range(0, _queue.WorkerCount)
                .Select(_ => Task.Run(RunWorkerAsync, _context.Cancellation))
                .ToArray();

            return Task.WhenAll(workers);
        }

        private async Task RunWorkerAsync()
        {
            while (true)
            {
                _context.Cancellation.ThrowIfCancellationRequested();
                var file = _queue.TakeNext();
                if (file == null)
                    return;

                await _context.DownloadAsync(file);
            }
        }

        internal static Task RunAsync(
            IReadOnlyList<DepotManifest.FileData> files,
            DepotFileDownloadContext context
        )
            => new DepotFileDownloadWorkers(
                new DepotFileDownloadQueue(files),
                context
            ).RunAsync();
    }

    private async Task DownloadDepotFilesAsync(
        IReadOnlyList<DepotManifest.FileData> filesToDownload,
        uint depotId,
        byte[] depotKey,
        CancellationToken ct
    )
    {
        var context = DepotFileDownloadContext.Create(this, depotId, depotKey, ct);
        await DepotFileDownloadWorkers.RunAsync(filesToDownload, context);
    }
}
