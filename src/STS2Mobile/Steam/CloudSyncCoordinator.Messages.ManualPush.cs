using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
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
}
