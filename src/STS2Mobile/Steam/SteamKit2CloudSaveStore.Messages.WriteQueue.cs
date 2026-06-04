using System;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private static readonly string WriteQueueDisposedDrop =
        CloudStoreMessage("Write queue is disposed; dropping queued write action");
    private static readonly string WriteQueueNullActionDrop =
        CloudStoreMessage("Attempted to enqueue null write action; dropping");
    private static readonly string WriteQueueClosingDrop =
        CloudStoreMessage("Write queue closing; dropping write action");
    private static readonly string FlushCompleted =
        CloudStoreMessage("Flush completed");
    private static readonly string FlushTimedOutDuringDispose =
        CloudStoreMessage("Flush timed out during dispose");
    private static readonly string WriteThreadStopTimedOut =
        CloudStoreMessage("Cloud write thread did not stop in time");
    private static readonly string WriteThreadStopped =
        CloudStoreMessage("Cloud write thread stopped");
    private const string BackgroundWriteOperation = "Background write";

    private static string BackgroundWriteFailed(Exception ex) =>
        OperationFailed(BackgroundWriteOperation, ex);

    private static string WriteQueueFull(int maxQueuedWrites, long droppedWrites) =>
        CloudStoreMessage(
            $"Write queue full ({maxQueuedWrites}); dropped write action (total dropped: {droppedWrites})"
        );

    private static string FlushingPendingWrites(long pendingWrites) =>
        CloudStoreMessage($"Flushing {pendingWrites} pending writes...");

    private static string FlushTimedOut(int queuedWrites, long pendingWrites) =>
        CloudStoreMessage(
            $"Flush timed out, {queuedWrites} queued + {pendingWrites} total pending writes"
        );

    private static string FlushDroppedWriteWarning(long droppedWrites) =>
        CloudStoreMessage($"Flush warning: {droppedWrites} actions were previously dropped");

    private static string TotalDroppedWrites(long droppedWrites) =>
        CloudStoreMessage($"Total dropped write actions: {droppedWrites}");
}
