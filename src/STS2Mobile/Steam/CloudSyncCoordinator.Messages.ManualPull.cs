using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
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
