using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudWriteQueue : IDisposable
    {
        private const int DisposeFlushTimeoutMs = 5000;
        private const int EnqueueTimeoutMs = 2500;
        private const int MaxQueuedWrites = 256;

        private readonly BlockingCollection<Action> _queue = new(
            new ConcurrentQueue<Action>(),
            MaxQueuedWrites
        );
        private readonly ManualResetEventSlim _drainSignal = new(initialState: true);
        private readonly Thread _thread;
        private long _droppedWrites;
        private bool _isDisposed;
        private long _pendingWrites;

        internal CloudWriteQueue()
        {
            _thread = new Thread(ProcessLoop)
            {
                IsBackground = true,
                Name = "CloudSaveWriter",
            };
            _thread.Start();
        }

        private long DroppedWrites => Volatile.Read(ref _droppedWrites);

        private bool IsDisposed => Volatile.Read(ref _isDisposed);

        private long PendingWrites => Volatile.Read(ref _pendingWrites);

        private void MarkDisposed()
            => Volatile.Write(ref _isDisposed, true);
    }

    private void EnqueueUpload(
        string canonPath,
        byte[] bytes,
        DateTimeOffset timestamp
    )
        => _writeQueue.Enqueue(
            () => UploadWithRetry(canonPath, bytes, timestamp: timestamp)
        );

    private void EnqueueDelete(string canonPath)
        => _writeQueue.Enqueue(() => DeleteCloudFileWithRetry(canonPath));

    private void EnqueueBatchUpload(IReadOnlyList<SaveBatchFile> files)
        => _writeQueue.Enqueue(() => UploadSaveBatch(files));
}
