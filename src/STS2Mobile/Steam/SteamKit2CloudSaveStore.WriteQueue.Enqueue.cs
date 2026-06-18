using System;
using System.Threading;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudWriteQueue
    {
        internal void Enqueue(Action action)
        {
            if (IsDisposed)
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
            TryAddQueuedWrite(action);
        }

        private void TryAddQueuedWrite(Action action)
        {
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
            catch (ObjectDisposedException)
            {
                DropQueuedWrite(WriteQueueClosingDrop);
            }
            catch (InvalidOperationException)
            {
                DropQueuedWrite(WriteQueueClosingDrop);
            }
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
    }
}
