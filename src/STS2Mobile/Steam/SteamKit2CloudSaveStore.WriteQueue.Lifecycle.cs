using System;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudWriteQueue
    {
        internal bool Flush(int timeoutMs = 5000)
        {
            if (_isDisposed)
                return true;

            return WaitForDrain(timeoutMs);
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
    }
}
