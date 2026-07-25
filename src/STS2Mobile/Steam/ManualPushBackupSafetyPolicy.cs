using System;

namespace STS2Mobile.Steam;

internal static class ManualPushBackupSafetyPolicy
{
    internal static void EnsureSatisfied(
        bool localBackupEnabled,
        bool hasStoragePermission,
        int importantLocalSaveCount,
        int localBackupCount,
        int importantCloudSaveCount,
        int cloudBackupCount
    )
    {
        if (!localBackupEnabled)
            return;

        if (!hasStoragePermission)
        {
            throw new InvalidOperationException(
                "Manual Push blocked: local backup is enabled but backup storage permission is unavailable."
            );
        }

        if (importantLocalSaveCount > 0 && localBackupCount < importantLocalSaveCount)
        {
            throw new InvalidOperationException(
                $"Manual Push blocked: local pre-Push backup evidence is incomplete for important Android saves ({localBackupCount}/{importantLocalSaveCount})."
            );
        }

        if (importantCloudSaveCount > 0 && cloudBackupCount < importantCloudSaveCount)
        {
            throw new InvalidOperationException(
                $"Manual Push blocked: cloud pre-Push backup evidence is incomplete for existing important Steam Cloud saves ({cloudBackupCount}/{importantCloudSaveCount})."
            );
        }
    }
}
