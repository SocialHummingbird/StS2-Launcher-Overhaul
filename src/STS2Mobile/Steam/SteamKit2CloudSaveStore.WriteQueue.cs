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

        private long PendingWrites => Volatile.Read(ref _pendingWrites);

        internal void Enqueue(Action action)
        {
            if (_isDisposed)
            {
                PatchHelper.Log(WriteQueueDisposedDrop);
                return;
            }

            if (action == null)
            {
                PatchHelper.Log(WriteQueueNullActionDrop);
                return;
            }

            MarkQueued();
            try
            {
                if (!_queue.TryAdd(action, EnqueueTimeoutMs))
                {
                    DropQueuedWrite(
                        dropped => WriteQueueFull(MaxQueuedWrites, dropped)
                    );
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                DropQueuedWrite(WriteQueueClosingDrop);
            }
        }

        internal bool Flush(int timeoutMs = 5000)
        {
            if (_isDisposed)
                return true;

            return WaitForDrain(timeoutMs);
        }

        private QueueDrainState CaptureDrainState()
            => new(_queue.Count, PendingWrites);

        private bool WaitForDrain(int timeoutMs)
        {
            var initialState = CaptureDrainState();
            if (!initialState.HasPendingWrites)
                return true;

            initialState.LogFlushStart();

            if (_drainSignal.Wait(timeoutMs))
            {
                PatchHelper.Log(FlushCompleted);
                return true;
            }

            var timedOutState = CaptureDrainState();
            timedOutState.LogFlushTimeout();
            if (DroppedWrites > 0)
                PatchHelper.Log(FlushDroppedWriteWarning(DroppedWrites));
            return false;
        }

        void IDisposable.Dispose()
            => Dispose();

        internal void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            var completed = WaitForDrain(DisposeFlushTimeoutMs);
            if (!completed)
                PatchHelper.Log(FlushTimedOutDuringDispose);

            _queue.CompleteAdding();
            if (!_thread.Join(3000))
                PatchHelper.Log(WriteThreadStopTimedOut);
            else
                PatchHelper.Log(WriteThreadStopped);

            if (DroppedWrites > 0)
                PatchHelper.Log(TotalDroppedWrites(DroppedWrites));

            _queue.Dispose();
            _drainSignal.Dispose();
        }

        private void MarkQueued()
        {
            _drainSignal.Reset();
            Interlocked.Increment(ref _pendingWrites);
        }

        private void MarkCompleted()
        {
            if (Interlocked.Decrement(ref _pendingWrites) <= 0)
                _drainSignal.Set();
        }

        private long MarkDroppedQueuedWrite()
        {
            var dropped = Interlocked.Increment(ref _droppedWrites);
            MarkCompleted();
            return dropped;
        }

        private void DropQueuedWrite(Func<long, string> logMessage)
            => PatchHelper.Log(logMessage(MarkDroppedQueuedWrite()));

        private void DropQueuedWrite(string logMessage)
            => DropQueuedWrite(_ => logMessage);

        private void ProcessLoop()
        {
            foreach (var action in _queue.GetConsumingEnumerable())
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(BackgroundWriteFailed(ex));
                }
                finally
                {
                    MarkCompleted();
                }
            }
        }
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
