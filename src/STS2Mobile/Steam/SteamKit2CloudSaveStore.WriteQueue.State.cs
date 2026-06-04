namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private sealed partial class CloudWriteQueue
    {
        private readonly struct QueueDrainState
        {
            internal QueueDrainState(int queuedWrites, long pendingWrites)
            {
                QueuedWrites = queuedWrites;
                PendingWrites = pendingWrites;
            }

            internal int QueuedWrites { get; }
            internal long PendingWrites { get; }
            internal bool HasPendingWrites => PendingWrites > 0 || QueuedWrites > 0;

            internal void LogFlushStart()
                => PatchHelper.Log(FlushingPendingWrites(PendingWrites));

            internal void LogFlushTimeout()
                => PatchHelper.Log(FlushTimedOut(QueuedWrites, PendingWrites));
        }
    }
}
