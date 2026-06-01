using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const string ManualPushBudgetExceeded =
        "[Cloud] Manual push timeout: exceeded overall manual sync budget";
    private const string ManualPullBudgetExceeded =
        "[Cloud] Manual pull timeout: exceeded overall manual sync budget";

    private static string SyncLocalCorruptPulling(string path) =>
        $"[Cloud] Sync: local {path} is corrupt, pulling from cloud";

    private static string SyncIdenticalSkipping(string path) =>
        $"[Cloud] Sync: {path} identical, skipping";

    private static string SyncCloudWins(string path) =>
        $"[Cloud] Sync: cloud wins for {path}";

    private static string SyncLocalWinsUploading(string path) =>
        $"[Cloud] Sync: local wins for {path}, uploading";

    private static string SyncContentsDifferCloudWins(string path) =>
        $"[Cloud] Sync: contents differ for {path}, cloud wins";

    private static string SyncFailed(string path, Exception ex) =>
        $"[Cloud] Sync failed for {path}: {ex.Message}";

    private static string ProgressComparisonFailed(string path, Exception ex) =>
        $"[Cloud] Progress comparison failed for {path}: {ex.Message}";

    private static string PushSkippingIdentical(string path) =>
        $"[Cloud] Push: skipping {path} (identical)";

    private static string PushUploaded(string path) =>
        $"[Cloud] Push: uploaded {path}";

    private static string PushStarting(int fileCount) =>
        $"[Cloud] Push: starting ({fileCount} files)";

    private static string PushBackedUpCloudFiles(int backedUp) =>
        $"[Cloud] Push: backed up {backedUp} cloud files";

    private static string PushQueuing(string path, int bytes) =>
        $"[Cloud] Push: queuing {path} ({bytes} bytes)";

    private static string PushFailed(string path, Exception ex) =>
        $"[Cloud] Push: failed for {path}: {ex.Message}";

    private static string PushComplete(int count) =>
        $"[Cloud] Push complete: {count} files batched for upload";

    private static string PushBackingUpCloud(string path) =>
        $"[Cloud] Push: backing up cloud {path}";

    private static string PushCloudBackupFailed(string path, Exception ex) =>
        $"[Cloud] Push: backup failed for cloud {path}: {ex.Message}";

    private static string PullSkippingIdentical(string path) =>
        $"[Cloud] Pull: skipping {path} (identical)";

    private static string PullDownloaded(string path) =>
        $"[Cloud] Pull: downloaded {path}";

    private static string PullStarting(int fileCount) =>
        $"[Cloud] Pull: starting ({fileCount} files)";

    private static string PullBackedUpLocalFiles(int backedUp) =>
        $"[Cloud] Pull: backed up {backedUp} local files";

    private static string PullDownloading(string path) =>
        $"[Cloud] Pull: downloading {path}";

    private static string PullWrote(string path, int bytes) =>
        $"[Cloud] Pull: wrote {path} ({bytes} bytes)";

    private static string PullPathTimedOut(string path) =>
        $"[Cloud] Pull: timeout for {path}, skipping remaining operations for this file";

    private static string PullFailed(string path, Exception ex) =>
        $"[Cloud] Pull: failed for {path}: {ex.Message}";

    private static string PullComplete(int downloaded, int skipped) =>
        $"[Cloud] Pull complete: {downloaded} downloaded, {skipped} not in cloud";

    private static string PullLocalBackupFailed(string path, Exception ex) =>
        $"[Cloud] Pull: backup failed for local {path}: {ex.Message}";

    private static string SaveBackedUp(string source, string path) =>
        $"[Cloud] Backed up {source} {path}";

    private static string SaveBackedUpTo(string source, string path, string backupPath) =>
        $"[Cloud] Backed up {source} {path} -> {backupPath}";

    private static string BackupFailed(string source, string path, Exception ex) =>
        $"[Cloud] Backup failed for {source} {path}: {ex.Message}";
}
