using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    internal static void SaveLocalBackupEnabled(bool enabled)
        => LocalBackupPreference.Save(enabled);

    private static void RequestStoragePermissionForLocalBackup(bool enabled)
    {
        if (enabled && !AppPaths.HasStoragePermission())
            AppPaths.RequestStoragePermission();
    }

    private static void ApplyLocalBackup(bool enabled)
    {
        CloudSyncCoordinator.SetLocalBackupEnabled(enabled);
        if (enabled)
            AppPaths.EnsureExternalDirectories();
    }
}
