namespace STS2Mobile.Steam;

// Stateless cloud sync coordinator: auto sync, manual push/pull, and save backups.
internal static partial class CloudSyncCoordinator
{
    private static bool _localBackupEnabled;

    internal static void SetLocalBackupEnabled(bool enabled)
    {
        _localBackupEnabled = enabled;
    }

    internal static void RefreshLocalBackup(bool restoreMissing)
    {
        if (!_localBackupEnabled)
            return;

        try
        {
            SaveBackups.RefreshLocalMirror(
                CloudSaveStoreFactory.CreateLocalStore(),
                restoreMissing
            );
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Cloud] Automatic local backup refresh failed: {ex.Message}");
        }
    }

    internal static void MirrorLocalSaveWrite(string path, byte[] content)
        => SaveBackups.MirrorLocalWrite(path, content);
}
