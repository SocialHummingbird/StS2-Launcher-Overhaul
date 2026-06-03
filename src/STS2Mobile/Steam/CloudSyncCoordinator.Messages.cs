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
        CloudOperationMessage(
            SyncOperation,
            $"local {path} is corrupt, pulling from cloud"
        );

    private static string SyncIdenticalSkipping(string path) =>
        CloudOperationMessage(SyncOperation, $"{path} identical, skipping");

    private static string SyncCloudWins(string path) =>
        CloudOperationMessage(SyncOperation, $"cloud wins for {path}");

    private static string SyncLocalWinsUploading(string path) =>
        CloudOperationMessage(SyncOperation, $"local wins for {path}, uploading");

    private static string SyncContentsDifferCloudWins(string path) =>
        CloudOperationMessage(SyncOperation, $"contents differ for {path}, cloud wins");

    private static string SyncFailed(string path, Exception ex) =>
        CloudMessage($"Sync failed for {path}: {ex.Message}");

    private static string ProgressComparisonFailed(string path, Exception ex) =>
        CloudMessage($"Progress comparison failed for {path}: {ex.Message}");

    private static string PushSkippingIdentical(string path) =>
        CloudOperationMessage(PushOperation, $"skipping {path} (identical)");

    private static string PushUploaded(string path) =>
        CloudOperationMessage(PushOperation, $"uploaded {path}");

    private static string PushStarting(int fileCount) =>
        CloudOperationMessage(PushOperation, $"starting ({fileCount} files)");

    private static string PushBackedUpCloudFiles(int backedUp) =>
        CloudOperationMessage(PushOperation, $"backed up {backedUp} cloud files");

    private static string PushQueuing(string path, int bytes) =>
        CloudOperationMessage(PushOperation, $"queuing {path} ({bytes} bytes)");

    private static string PushFailed(string path, Exception ex) =>
        CloudFailureMessage(PushOperation, path, ex);

    private static string PushComplete(int count) =>
        CloudMessage($"Push complete: {count} files batched for upload");

    private static string PushBackingUpCloud(string path) =>
        CloudOperationMessage(PushOperation, $"backing up cloud {path}");

    private static string PushCloudBackupFailed(string path, Exception ex) =>
        CloudOperationMessage(
            PushOperation,
            $"backup failed for cloud {path}: {ex.Message}"
        );

    private static string PullSkippingIdentical(string path) =>
        CloudOperationMessage(PullOperation, $"skipping {path} (identical)");

    private static string PullDownloaded(string path) =>
        CloudOperationMessage(PullOperation, $"downloaded {path}");

    private static string PullStarting(int fileCount) =>
        CloudOperationMessage(PullOperation, $"starting ({fileCount} files)");

    private static string PullBackedUpLocalFiles(int backedUp) =>
        CloudOperationMessage(PullOperation, $"backed up {backedUp} local files");

    private static string PullDownloading(string path) =>
        CloudOperationMessage(PullOperation, $"downloading {path}");

    private static string PullWrote(string path, int bytes) =>
        CloudOperationMessage(PullOperation, $"wrote {path} ({bytes} bytes)");

    private static string PullPathTimedOut(string path) =>
        CloudOperationMessage(
            PullOperation,
            $"timeout for {path}, skipping remaining operations for this file"
        );

    private static string PullFailed(string path, Exception ex) =>
        CloudFailureMessage(PullOperation, path, ex);

    private static string PullComplete(int downloaded, int skipped) =>
        CloudMessage($"Pull complete: {downloaded} downloaded, {skipped} not in cloud");

    private static string PullLocalBackupFailed(string path, Exception ex) =>
        CloudOperationMessage(
            PullOperation,
            $"backup failed for local {path}: {ex.Message}"
        );

    private static string SaveBackedUp(string source, string path) =>
        CloudMessage($"Backed up {source} {path}");

    private static string SaveBackedUpTo(string source, string path, string backupPath) =>
        CloudMessage($"Backed up {source} {path} -> {backupPath}");

    private static string BackupFailed(string source, string path, Exception ex) =>
        CloudMessage($"Backup failed for {source} {path}: {ex.Message}");

    private static string CloudFailureMessage(
        string operation,
        string path,
        Exception ex
    )
        => CloudOperationMessage(operation, $"failed for {path}: {ex.Message}");

    private static string CloudOperationMessage(string operation, string message)
        => CloudMessage($"{operation}: {message}");

    private static string CloudMessage(string message)
        => $"{CloudLogPrefix} {message}";
}
