using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static string PushBackingUpCloud(string path) =>
        PushMessages.Format($"backing up cloud {path}");

    private static string PushCloudBackupFailed(string path, Exception ex) =>
        PushMessages.Format($"backup failed for cloud {path}: {ex.Message}");

    private static string PullLocalBackupFailed(string path, Exception ex) =>
        PullMessages.Format($"backup failed for local {path}: {ex.Message}");

    private static string SaveBackedUp(string source, string path) =>
        CloudMessage($"Backed up {source} {path}");

    private static string SaveBackedUpTo(string source, string path, string backupPath) =>
        CloudMessage($"Backed up {source} {path} -> {backupPath}");

    private static string BackupFailed(string source, string path, Exception ex) =>
        CloudMessage($"Backup failed for {source} {path}: {ex.Message}");
}
