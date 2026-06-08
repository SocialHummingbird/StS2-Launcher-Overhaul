using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static string SyncLocalCorruptPulling(string path) =>
        SyncMessage($"local {path} is corrupt, pulling from cloud");

    private static string SyncIdenticalSkipping(string path) =>
        SyncMessage($"{path} identical, skipping");

    private static string SyncCloudWins(string path) =>
        SyncMessage($"cloud wins for {path}");

    private static string SyncLocalWinsUploading(string path) =>
        SyncMessage($"local wins for {path}, uploading");

    private static string SyncCloudMissingLocalWins(string path) =>
        SyncMessage($"cloud missing for {path}, uploading local");

    private static string SyncCloudMissingSkipping(string path) =>
        SyncMessage($"cloud missing for {path}, skipping");

    private static string SyncContentsDifferCloudWins(string path) =>
        SyncMessage($"contents differ for {path}, cloud wins");

    private static string SyncFailed(string path, Exception ex) =>
        CloudMessage($"Sync failed for {path}: {ex.Message}");

    private static string SaveComparisonFailed(string path, Exception ex) =>
        CloudMessage($"Save comparison failed for {path}: {ex.Message}");
}
