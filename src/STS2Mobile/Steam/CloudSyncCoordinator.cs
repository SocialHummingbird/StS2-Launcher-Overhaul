namespace STS2Mobile.Steam;

// Stateless cloud sync coordinator: auto sync, manual push/pull, and save backups.
internal static partial class CloudSyncCoordinator
{
    private static bool _localBackupEnabled;

    internal static void SetLocalBackupEnabled(bool enabled)
    {
        _localBackupEnabled = enabled;
    }
}
