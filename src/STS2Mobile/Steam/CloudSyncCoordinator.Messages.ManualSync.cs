using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const string ManualPushBudgetExceeded =
        "[Cloud] Manual push timeout: exceeded overall manual sync budget";
    private const string ManualPullBudgetExceeded =
        "[Cloud] Manual pull timeout: exceeded overall manual sync budget";

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
        PullMessages.Format(
            $"timeout for {path}, skipping remaining operations for this file"
        );

    private static string PullFailed(string path, Exception ex) =>
        PullMessages.Format($"failed for {path}: {ex.Message}");

    private static string PullComplete(int downloaded, int skipped) =>
        PullMessages.Format($"complete: {downloaded} downloaded, {skipped} not in cloud");
}
