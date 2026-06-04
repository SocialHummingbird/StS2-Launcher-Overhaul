using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
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

    private static string SaveComparisonFailed(string path, Exception ex) =>
        CloudMessage($"Save comparison failed for {path}: {ex.Message}");
}
