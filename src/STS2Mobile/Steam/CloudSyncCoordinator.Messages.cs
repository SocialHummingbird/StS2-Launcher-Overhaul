using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const string ManualPushBudgetExceeded =
        "[Cloud] Manual push timeout: exceeded overall manual sync budget";
    private const string ManualPullBudgetExceeded =
        "[Cloud] Manual pull timeout: exceeded overall manual sync budget";
    private const string CloudLogPrefix = "[Cloud]";
    private const string PullOperation = "Pull";
    private const string PushOperation = "Push";
    private const string SyncOperation = "Sync";

    private static string SyncLocalCorruptPulling(string path) =>
        SyncMessage($"local {path} is corrupt, pulling from cloud");

    private static string SyncIdenticalSkipping(string path) =>
        SyncMessage($"{path} identical, skipping");

    private static string SyncCloudWins(string path) =>
        SyncMessage($"cloud wins for {path}");

    private static string SyncLocalWinsUploading(string path) =>
        SyncMessage($"local wins for {path}, uploading");

    private static string SyncContentsDifferCloudWins(string path) =>
        SyncMessage($"contents differ for {path}, cloud wins");

    private static string SyncFailed(string path, Exception ex) =>
        CloudMessage($"Sync failed for {path}: {ex.Message}");

    private static string ProgressComparisonFailed(string path, Exception ex) =>
        CloudMessage($"Progress comparison failed for {path}: {ex.Message}");

    private static string PushUploaded(string path) =>
        PushMessage($"uploaded {path}");

    private static string PushStarting(int fileCount) =>
        PushMessage($"starting ({fileCount} files)");

    private static string PushBackedUpCloudFiles(int backedUp) =>
        PushMessage($"backed up {backedUp} cloud files");

    private static string PushQueuing(string path, int bytes) =>
        PushMessage($"queuing {path} ({bytes} bytes)");

    private static string PushFailed(string path, Exception ex) =>
        PushMessage($"failed for {path}: {ex.Message}");

    private static string PushComplete(int count) =>
        CloudMessage($"Push complete: {count} files batched for upload");

    private static string PushBackingUpCloud(string path) =>
        PushMessage($"backing up cloud {path}");

    private static string PushCloudBackupFailed(string path, Exception ex) =>
        PushMessage($"backup failed for cloud {path}: {ex.Message}");

    private static string PullDownloaded(string path) =>
        PullMessage($"downloaded {path}");

    private static string PullStarting(int fileCount) =>
        PullMessage($"starting ({fileCount} files)");

    private static string PullBackedUpLocalFiles(int backedUp) =>
        PullMessage($"backed up {backedUp} local files");

    private static string PullDownloading(string path) =>
        PullMessage($"downloading {path}");

    private static string PullWrote(string path, int bytes) =>
        PullMessage($"wrote {path} ({bytes} bytes)");

    private static string PullPathTimedOut(string path) =>
        PullMessage($"timeout for {path}, skipping remaining operations for this file");

    private static string PullFailed(string path, Exception ex) =>
        PullMessage($"failed for {path}: {ex.Message}");

    private static string PullComplete(int downloaded, int skipped) =>
        CloudMessage($"Pull complete: {downloaded} downloaded, {skipped} not in cloud");

    private static string PullLocalBackupFailed(string path, Exception ex) =>
        PullMessage($"backup failed for local {path}: {ex.Message}");

    private static string SaveBackedUp(string source, string path) =>
        CloudMessage($"Backed up {source} {path}");

    private static string SaveBackedUpTo(string source, string path, string backupPath) =>
        CloudMessage($"Backed up {source} {path} -> {backupPath}");

    private static string BackupFailed(string source, string path, Exception ex) =>
        CloudMessage($"Backup failed for {source} {path}: {ex.Message}");

    private static string SyncMessage(string message)
        => OperationMessage(SyncOperation, message);

    private static string PushMessage(string message)
        => OperationMessage(PushOperation, message);

    private static string PullMessage(string message)
        => OperationMessage(PullOperation, message);

    private static string OperationMessage(string operation, string message)
        => CloudMessage($"{operation}: {message}");

    private static string CloudMessage(string message)
        => $"{CloudLogPrefix} {message}";
}
