using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static string PushBackingUpCloud(string path) =>
            CloudSyncCoordinator.PushMessages.Format($"backing up cloud {path}");

        private static string PushCloudBackupFailed(string path, Exception ex) =>
            CloudSyncCoordinator.PushMessages.Format(
                $"backup failed for cloud {path}: {ex.Message}"
            );

        private static string PullLocalBackupFailed(string path, Exception ex) =>
            CloudSyncCoordinator.PullMessages.Format(
                $"backup failed for local {path}: {ex.Message}"
            );

        private static string SaveBackedUp(BackupSource source, string path) =>
            CloudSyncCoordinator.CloudMessage($"Backed up {source} {path}");

        private static string SaveBackedUpTo(
            BackupSource source,
            string path,
            string backupPath
        ) =>
            CloudSyncCoordinator.CloudMessage(
                $"Backed up {source} {path} -> {backupPath}"
            );

        private static string BackupFailed(
            BackupSource source,
            string path,
            Exception ex
        ) =>
            CloudSyncCoordinator.CloudMessage(
                $"Backup failed for {source} {path}: {ex.Message}"
            );
    }
}
