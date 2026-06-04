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

    private static readonly CloudOperationMessages PullMessages = new(PullOperation);
    private static readonly CloudOperationMessages PushMessages = new(PushOperation);
    private static readonly CloudOperationMessages SyncMessages = new(SyncOperation);

    private readonly struct CloudOperationMessages
    {
        private readonly string _operation;

        internal CloudOperationMessages(string operation)
        {
            _operation = operation;
        }

        internal string Format(string message)
            => CloudMessage($"{_operation}: {message}");
    }

    private static string SyncLocalCorruptPulling(string path) =>
        SyncMessages.Format($"local {path} is corrupt, pulling from cloud");

    private static string SyncIdenticalSkipping(string path) =>
        SyncMessages.Format($"{path} identical, skipping");

    private static string SyncCloudWins(string path) =>
        SyncMessages.Format($"cloud wins for {path}");

    private static string SyncLocalWinsUploading(string path) =>
        SyncMessages.Format($"local wins for {path}, uploading");

    private static string SyncContentsDifferCloudWins(string path) =>
        SyncMessages.Format($"contents differ for {path}, cloud wins");

    private static string SyncFailed(string path, Exception ex) =>
        CloudMessage($"Sync failed for {path}: {ex.Message}");

    private static string ProgressComparisonFailed(string path, Exception ex) =>
        CloudMessage($"Progress comparison failed for {path}: {ex.Message}");

    private static string PushUploaded(string path) =>
        PushMessages.Format($"uploaded {path}");

    private static string PushStarting(int fileCount) =>
        PushMessages.Format($"starting ({fileCount} files)");

    private static string PushBackedUpCloudFiles(int backedUp) =>
        PushMessages.Format($"backed up {backedUp} cloud files");

    private static string PushQueuing(string path, int bytes) =>
        PushMessages.Format($"queuing {path} ({bytes} bytes)");

    private static string PushFailed(string path, Exception ex) =>
        PushMessages.Format($"failed for {path}: {ex.Message}");

    private static string PushComplete(int count) =>
        PushMessages.Format($"complete: {count} files batched for upload");

    private static string PushBackingUpCloud(string path) =>
        PushMessages.Format($"backing up cloud {path}");

    private static string PushCloudBackupFailed(string path, Exception ex) =>
        PushMessages.Format($"backup failed for cloud {path}: {ex.Message}");

    private static string PullDownloaded(string path) =>
        PullMessages.Format($"downloaded {path}");

    private static string PullStarting(int fileCount) =>
        PullMessages.Format($"starting ({fileCount} files)");

    private static string PullBackedUpLocalFiles(int backedUp) =>
        PullMessages.Format($"backed up {backedUp} local files");

    private static string PullDownloading(string path) =>
        PullMessages.Format($"downloading {path}");

    private static string PullWrote(string path, int bytes) =>
        PullMessages.Format($"wrote {path} ({bytes} bytes)");

    private static string PullPathTimedOut(string path) =>
        PullMessages.Format($"timeout for {path}, skipping remaining operations for this file");

    private static string PullFailed(string path, Exception ex) =>
        PullMessages.Format($"failed for {path}: {ex.Message}");

    private static string PullComplete(int downloaded, int skipped) =>
        PullMessages.Format($"complete: {downloaded} downloaded, {skipped} not in cloud");

    private static string PullLocalBackupFailed(string path, Exception ex) =>
        PullMessages.Format($"backup failed for local {path}: {ex.Message}");

    private static string SaveBackedUp(string source, string path) =>
        CloudMessage($"Backed up {source} {path}");

    private static string SaveBackedUpTo(string source, string path, string backupPath) =>
        CloudMessage($"Backed up {source} {path} -> {backupPath}");

    private static string BackupFailed(string source, string path, Exception ex) =>
        CloudMessage($"Backup failed for {source} {path}: {ex.Message}");

    private static string CloudMessage(string message)
        => $"{CloudLogPrefix} {message}";
}
