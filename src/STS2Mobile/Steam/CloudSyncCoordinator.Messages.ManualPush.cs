using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static string PushUploaded(string path) =>
        PushMessage($"uploaded {path}");

    private static string PushStarting(int fileCount) =>
        PushMessage($"starting ({fileCount} files)");

    private static string PushBackedUpPrePushFiles(int backedUp) =>
        PushMessage($"backed up {backedUp} pre-push safety files");

    private static string PushQueuing(string path, int bytes) =>
        PushMessage($"queuing {path} ({bytes} bytes)");

    private static string PushFailed(string path, Exception ex) =>
        PushMessage($"failed for {path}: {ex.Message}");

    private static string PushComplete(int count) =>
        PushMessage($"complete: {count} files uploaded and flushed");

    private static string PushFlushing() =>
        PushMessage("waiting for queued cloud uploads to flush");

    private static string PushFlushTimedOut(string result) =>
        $"{result}; queued upload flush timed out";
}
