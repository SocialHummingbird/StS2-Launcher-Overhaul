using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
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
        PullMessage(
            $"timeout for {path}, skipping remaining operations for this file"
        );

    private static string PullFailed(string path, Exception ex) =>
        PullMessage($"failed for {path}: {ex.Message}");

    private static string PullNotInCloud(string path) =>
        PullMessage($"not in cloud: {path}");

    private static string PullComplete(int downloaded, int skipped) =>
        downloaded > 0
            ? PullMessage($"complete: {downloaded} downloaded, {skipped} not in cloud")
            : PullMessage(
                $"complete with no downloads: {skipped} candidate paths were not found in cloud; check the candidate path and cloud enumeration logs"
            );
}
