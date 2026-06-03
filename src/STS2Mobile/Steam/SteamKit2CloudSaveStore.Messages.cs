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

    private static string BeginSaveBatchFailed(Exception ex) =>
        OperationFailed("BeginSaveBatch", ex);

    private static string EndSaveBatchFailed(Exception ex) =>
        OperationFailed("EndSaveBatch", ex);

    private static string OperationThrottled(string operationName, string path, int delayMs) =>
        CloudStoreMessage(
            $"{operationName} throttled for {path}, retrying in {delayMs / 1000}s..."
        );

    private static string OperationFailed(string operationName, string path, Exception ex) =>
        CloudStoreMessage($"{operationName} failed for {path}: {ex.Message}");

    private static string QueueFlushFailed(Exception ex) =>
        OperationFailed("Queue flush", ex);

    private static string ConnectionFlushFailed(Exception ex) =>
        OperationFailed("Connection flush", ex);

    private static string Downloaded(
        string path,
        int bytes,
        bool encrypted,
        ulong fileSize,
        ulong rawFileSize) =>
        CloudStoreMessage(
            $"Downloaded {path} ({bytes} bytes, encrypted={encrypted}, file_size={fileSize}, raw_file_size={rawFileSize})"
        );

    private static string Unzipped(string path, int compressedSize, int decompressedSize) =>
        CloudStoreMessage($"Unzipped {path} ({compressedSize} -> {decompressedSize} bytes)");

    private static string RenameDeleteFailed(string sourcePath, Exception ex) =>
        CloudStoreMessage(
            $"RenameFile: delete of {sourcePath} failed (duplicate may exist): {ex.Message}"
        );

    private static string Compressed(string path, uint rawSize, int compressedSize) =>
        CloudStoreMessage($"Compressed {path} ({rawSize} -> {compressedSize} bytes)");

    private static string UploadingUncompressed(string path, uint rawSize) =>
        CloudStoreMessage($"Uploading {path} uncompressed ({rawSize} bytes)");

    private static string UploadSkippedAlreadyUpToDate(string path) =>
        CloudStoreMessage($"Skipped upload for {path} (already up to date)");

    private static string CommitReturnedFalse(string path) =>
        CloudStoreMessage($"Commit returned file_committed=false for {path}");

    private static string CommitFailed(string path, Exception ex) =>
        CloudStoreMessage($"Commit failed for {path}: {ex.Message}");

    private static string Wrote(string path, int bytes, bool compressed) =>
        CloudStoreMessage($"Wrote {bytes} bytes to {path} (compressed={compressed})");

    private static string BackgroundWriteFailed(Exception ex) =>
        OperationFailed("Background write", ex);

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

    private static string OperationFailed(string operationName, Exception ex) =>
        CloudStoreMessage($"{operationName} failed: {ex.Message}");

    private static string CloudStoreMessage(string message)
        => $"[Cloud] {message}";
}
