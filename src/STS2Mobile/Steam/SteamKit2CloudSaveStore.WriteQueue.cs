using System;
using System.Collections.Concurrent;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed class CloudWriteQueue : IDisposable
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

        public CloudWriteQueue()
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

        public void Enqueue(Action action)
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
                    var dropped = MarkDroppedQueuedWrite();
                    PatchHelper.Log(WriteQueueFull(MaxQueuedWrites, dropped));
                    return;
                }
            }
            catch (InvalidOperationException)
            {
                PatchHelper.Log(WriteQueueClosingDrop);
                MarkDroppedQueuedWrite();
            }
        }

        public bool Flush(int timeoutMs = 5000)
        {
            if (_isDisposed)
                return true;

            return WaitForDrain(timeoutMs);
        }

        private bool HasPendingWrites(long pending)
            => pending > 0 || _queue.Count > 0;

        private bool WaitForDrain(int timeoutMs)
        {
            var pending = PendingWrites;
            if (!HasPendingWrites(pending))
                return true;

            PatchHelper.Log(FlushingPendingWrites(pending));

            if (_drainSignal.Wait(timeoutMs))
            {
                PatchHelper.Log(FlushCompleted);
                return true;
            }

            PatchHelper.Log(FlushTimedOut(_queue.Count, PendingWrites));
            if (DroppedWrites > 0)
                PatchHelper.Log(FlushDroppedWriteWarning(DroppedWrites));
            return false;
        }

        void IDisposable.Dispose()
            => Dispose();

        public void Dispose()
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
}
