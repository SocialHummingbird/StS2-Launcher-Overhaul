using System;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private const string WriteQueueDisposedDrop =
        "[Cloud] Write queue is disposed; dropping queued write action";
    private const string WriteQueueNullActionDrop =
        "[Cloud] Attempted to enqueue null write action; dropping";
    private const string WriteQueueClosingDrop =
        "[Cloud] Write queue closing; dropping write action";
    private const string FlushCompleted = "[Cloud] Flush completed";
    private const string FlushTimedOutDuringDispose =
        "[Cloud] Flush timed out during dispose";
    private const string WriteThreadStopTimedOut =
        "[Cloud] Cloud write thread did not stop in time";
    private const string WriteThreadStopped = "[Cloud] Cloud write thread stopped";

    private static string BeginSaveBatchFailed(Exception ex) =>
        $"[Cloud] BeginSaveBatch failed: {ex.Message}";

    private static string EndSaveBatchFailed(Exception ex) =>
        $"[Cloud] EndSaveBatch failed: {ex.Message}";

    private static string OperationThrottled(string operationName, string path, int delayMs) =>
        $"[Cloud] {operationName} throttled for {path}, retrying in {delayMs / 1000}s...";

    private static string OperationFailed(string operationName, string path, Exception ex) =>
        $"[Cloud] {operationName} failed for {path}: {ex.Message}";

    private static string QueueFlushFailed(Exception ex) =>
        $"[Cloud] Queue flush failed: {ex.Message}";

    private static string ConnectionFlushFailed(Exception ex) =>
        $"[Cloud] Connection flush failed: {ex.Message}";

    private static string Downloaded(
        string path,
        int bytes,
        bool encrypted,
        ulong fileSize,
        ulong rawFileSize) =>
        $"[Cloud] Downloaded {path} ({bytes} bytes, encrypted={encrypted}, file_size={fileSize}, raw_file_size={rawFileSize})";

    private static string Unzipped(string path, int compressedSize, int decompressedSize) =>
        $"[Cloud] Unzipped {path} ({compressedSize} -> {decompressedSize} bytes)";

    private static string RenameDeleteFailed(string sourcePath, Exception ex) =>
        $"[Cloud] RenameFile: delete of {sourcePath} failed (duplicate may exist): {ex.Message}";

    private static string Compressed(string path, uint rawSize, int compressedSize) =>
        $"[Cloud] Compressed {path} ({rawSize} -> {compressedSize} bytes)";

    private static string UploadingUncompressed(string path, uint rawSize) =>
        $"[Cloud] Uploading {path} uncompressed ({rawSize} bytes)";

    private static string UploadSkippedAlreadyUpToDate(string path) =>
        $"[Cloud] Skipped upload for {path} (already up to date)";

    private static string CommitReturnedFalse(string path) =>
        $"[Cloud] Commit returned file_committed=false for {path}";

    private static string CommitFailed(string path, Exception ex) =>
        $"[Cloud] Commit failed for {path}: {ex.Message}";

    private static string Wrote(string path, int bytes, bool compressed) =>
        $"[Cloud] Wrote {bytes} bytes to {path} (compressed={compressed})";

    private static string BackgroundWriteFailed(Exception ex) =>
        $"[Cloud] Background write failed: {ex.Message}";

    private static string WriteQueueFull(int maxQueuedWrites, long droppedWrites) =>
        $"[Cloud] Write queue full ({maxQueuedWrites}); dropped write action (total dropped: {droppedWrites})";

    private static string FlushingPendingWrites(long pendingWrites) =>
        $"[Cloud] Flushing {pendingWrites} pending writes...";

    private static string FlushTimedOut(int queuedWrites, long pendingWrites) =>
        $"[Cloud] Flush timed out, {queuedWrites} queued + {pendingWrites} total pending writes";

    private static string FlushDroppedWriteWarning(long droppedWrites) =>
        $"[Cloud] Flush warning: {droppedWrites} actions were previously dropped";

    private static string TotalDroppedWrites(long droppedWrites) =>
        $"[Cloud] Total dropped write actions: {droppedWrites}";
}
