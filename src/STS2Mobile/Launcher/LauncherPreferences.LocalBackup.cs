using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    internal static bool ReadLocalBackupEnabled()
        => LocalBackupPreference.Read();

    internal static void SaveLocalBackupEnabled(bool enabled)
        => LocalBackupPreference.Save(enabled);

    internal static LocalBackupRefreshResult
        SaveLocalBackupEnabledWithResult(bool enabled)
        => LocalBackupPreference.SaveWithResult(
            enabled,
            ApplyLocalBackupWithResult
        );

    internal static bool LoadAndApplyLocalBackupEnabled()
        => LocalBackupPreference.LoadAndApply();

    private static void RequestStoragePermissionForLocalBackup(bool enabled)
    {
        if (enabled && !AppPaths.HasStoragePermission())
            AppPaths.RequestStoragePermission();
    }

    private static void ApplyLocalBackup(bool enabled)
        => _ = ApplyLocalBackupWithResult(enabled);

    private static LocalBackupRefreshResult ApplyLocalBackupWithResult(
        bool enabled
    )
    {
        CloudSyncCoordinator.SetLocalBackupEnabled(enabled);
        if (!enabled)
            return LocalBackupRefreshResult.Skipped(
                AppPaths.HasStoragePermission()
            );

        AppPaths.EnsureExternalDirectories();
        return CloudSyncCoordinator.RefreshLocalBackup();
    }
}
