using System;

namespace STS2Mobile.Steam;

internal static class CloudRuntimeMessage
{
    internal const string WriteQueueDisposedDrop =
        "[Cloud] Write queue is disposed; dropping queued write action";
    internal const string WriteQueueNullActionDrop =
        "[Cloud] Attempted to enqueue null write action; dropping";
    internal const string WriteQueueClosingDrop =
        "[Cloud] Write queue closing; dropping write action";
    internal const string FlushCompleted = "[Cloud] Flush completed";
    internal const string FlushTimedOutDuringDispose = "[Cloud] Flush timed out during dispose";
    internal const string WriteThreadStopTimedOut = "[Cloud] Cloud write thread did not stop in time";
    internal const string WriteThreadStopped = "[Cloud] Cloud write thread stopped";
    internal const string FileEnumerationMaxRetriesReached =
        "[Cloud] Max retries reached for cloud file enumeration this session.";
    internal const string ManualPushBudgetExceeded =
        "[Cloud] Manual push timeout: exceeded overall manual sync budget";
    internal const string ManualPullBudgetExceeded =
        "[Cloud] Manual pull timeout: exceeded overall manual sync budget";

    internal static string ProgressComparisonFailed(string path, Exception ex) =>
        $"[Cloud] Progress comparison failed for {path}: {ex.Message}";

    internal static string BackgroundWriteFailed(Exception ex) =>
        $"[Cloud] Background write failed: {ex.Message}";

    internal static string WriteQueueFull(int maxQueuedWrites, long droppedWrites) =>
        $"[Cloud] Write queue full ({maxQueuedWrites}); dropped write action (total dropped: {droppedWrites})";

    internal static string FlushingPendingWrites(long pendingWrites) =>
        $"[Cloud] Flushing {pendingWrites} pending writes...";

    internal static string FlushTimedOut(int queuedWrites, long pendingWrites) =>
        $"[Cloud] Flush timed out, {queuedWrites} queued + {pendingWrites} total pending writes";

    internal static string FlushDroppedWriteWarning(long droppedWrites) =>
        $"[Cloud] Flush warning: {droppedWrites} actions were previously dropped";

    internal static string TotalDroppedWrites(long droppedWrites) =>
        $"[Cloud] Total dropped write actions: {droppedWrites}";

    internal static string SaveBackedUp(string source, string path) =>
        $"[Cloud] Backed up {source} {path}";

    internal static string SaveBackedUpTo(string source, string path, string backupPath) =>
        $"[Cloud] Backed up {source} {path} -> {backupPath}";

    internal static string BackupFailed(string source, string path, Exception ex) =>
        $"[Cloud] Backup failed for {source} {path}: {ex.Message}";

    internal static string PushBackingUpCloud(string path) =>
        $"[Cloud] Push: backing up cloud {path}";

    internal static string PushCloudBackupFailed(string path, Exception ex) =>
        $"[Cloud] Push: backup failed for cloud {path}: {ex.Message}";

    internal static string PullLocalBackupFailed(string path, Exception ex) =>
        $"[Cloud] Pull: backup failed for local {path}: {ex.Message}";

    internal static string FileEnumerationFailed(int attempt, int maxRetries, Exception ex) =>
        $"[Cloud] Failed to enumerate cloud files (attempt {attempt}/{maxRetries}): {ex.Message}";

    internal static string LateCompletionAfterTimeout(string operation, AggregateException exception) =>
        $"[Cloud] Late completion after timeout for '{operation}', result: {exception?.GetBaseException().Message}";

    internal static string OperationThrottled(string operationName, string path, int delayMs) =>
        $"[Cloud] {operationName} throttled for {path}, retrying in {delayMs / 1000}s...";

    internal static string OperationFailed(string operationName, string path, Exception ex) =>
        $"[Cloud] {operationName} failed for {path}: {ex.Message}";

    internal static string FilesEnumerated(int count) =>
        $"[Cloud] Enumerated {count} cloud files";

    internal static string QueueFlushFailed(Exception ex) =>
        $"[Cloud] Queue flush failed: {ex.Message}";

    internal static string ConnectionFlushFailed(Exception ex) =>
        $"[Cloud] Connection flush failed: {ex.Message}";

    internal static string Downloaded(
        string path,
        int bytes,
        bool encrypted,
        ulong fileSize,
        ulong rawFileSize) =>
        $"[Cloud] Downloaded {path} ({bytes} bytes, encrypted={encrypted}, file_size={fileSize}, raw_file_size={rawFileSize})";

    internal static string Unzipped(string path, int compressedSize, int decompressedSize) =>
        $"[Cloud] Unzipped {path} ({compressedSize} -> {decompressedSize} bytes)";

    internal static string RenameDeleteFailed(string sourcePath, Exception ex) =>
        $"[Cloud] RenameFile: delete of {sourcePath} failed (duplicate may exist): {ex.Message}";

    internal static string BeginSaveBatchFailed(Exception ex) =>
        $"[Cloud] BeginSaveBatch failed: {ex.Message}";

    internal static string EndSaveBatchFailed(Exception ex) =>
        $"[Cloud] EndSaveBatch failed: {ex.Message}";

    internal static string Compressed(string path, uint rawSize, int compressedSize) =>
        $"[Cloud] Compressed {path} ({rawSize} -> {compressedSize} bytes)";

    internal static string UploadingUncompressed(string path, uint rawSize) =>
        $"[Cloud] Uploading {path} uncompressed ({rawSize} bytes)";

    internal static string UploadSkippedAlreadyUpToDate(string path) =>
        $"[Cloud] Skipped upload for {path} (already up to date)";

    internal static string CommitReturnedFalse(string path) =>
        $"[Cloud] Commit returned file_committed=false for {path}";

    internal static string CommitFailed(string path, Exception ex) =>
        $"[Cloud] Commit failed for {path}: {ex.Message}";

    internal static string Wrote(string path, int bytes, bool compressed) =>
        $"[Cloud] Wrote {bytes} bytes to {path} (compressed={compressed})";

    internal static string PushSkippingIdentical(string path) =>
        $"[Cloud] Push: skipping {path} (identical)";

    internal static string PushUploaded(string path) =>
        $"[Cloud] Push: uploaded {path}";

    internal static string PullSkippingIdentical(string path) =>
        $"[Cloud] Pull: skipping {path} (identical)";

    internal static string PullDownloaded(string path) =>
        $"[Cloud] Pull: downloaded {path}";

    internal static string SyncLocalCorruptPulling(string path) =>
        $"[Cloud] Sync: local {path} is corrupt, pulling from cloud";

    internal static string SyncIdenticalSkipping(string path) =>
        $"[Cloud] Sync: {path} identical, skipping";

    internal static string SyncCloudWins(string path) =>
        $"[Cloud] Sync: cloud wins for {path}";

    internal static string SyncLocalWinsUploading(string path) =>
        $"[Cloud] Sync: local wins for {path}, uploading";

    internal static string SyncContentsDifferCloudWins(string path) =>
        $"[Cloud] Sync: contents differ for {path}, cloud wins";

    internal static string SyncFailed(string path, Exception ex) =>
        $"[Cloud] Sync failed for {path}: {ex.Message}";

    internal static string PushStarting(int fileCount) =>
        $"[Cloud] Push: starting ({fileCount} files)";

    internal static string PushBackedUpCloudFiles(int backedUp) =>
        $"[Cloud] Push: backed up {backedUp} cloud files";

    internal static string PushQueuing(string path, int bytes) =>
        $"[Cloud] Push: queuing {path} ({bytes} bytes)";

    internal static string PushFailed(string path, Exception ex) =>
        $"[Cloud] Push: failed for {path}: {ex.Message}";

    internal static string PushComplete(int count) =>
        $"[Cloud] Push complete: {count} files batched for upload";

    internal static string PullStarting(int fileCount) =>
        $"[Cloud] Pull: starting ({fileCount} files)";

    internal static string PullBackedUpLocalFiles(int backedUp) =>
        $"[Cloud] Pull: backed up {backedUp} local files";

    internal static string PullDownloading(string path) =>
        $"[Cloud] Pull: downloading {path}";

    internal static string PullWrote(string path, int bytes) =>
        $"[Cloud] Pull: wrote {path} ({bytes} bytes)";

    internal static string PullPathTimedOut(string path) =>
        $"[Cloud] Pull: timeout for {path}, skipping remaining operations for this file";

    internal static string PullFailed(string path, Exception ex) =>
        $"[Cloud] Pull: failed for {path}: {ex.Message}";

    internal static string PullComplete(int downloaded, int skipped) =>
        $"[Cloud] Pull complete: {downloaded} downloaded, {skipped} not in cloud";

    internal static string SavePathManagerFallback(Exception ex) =>
        $"[Cloud] Save path manager failed, using Android fallback paths: {ex.Message}";
}
