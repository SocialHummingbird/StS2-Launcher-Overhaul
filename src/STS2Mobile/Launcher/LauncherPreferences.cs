using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private const string LocalBackupPreference = "local_backup_enabled";
    private const string CloudSyncPreference = "cloud_sync_enabled";

    internal static bool ReadLocalBackupEnabled()
        => LoadBoolean(LocalBackupPreference, defaultValue: false);

    internal static bool LoadAndApplyLocalBackupEnabled()
    {
        var enabled = ReadLocalBackupEnabled();
        ApplyLocalBackup(enabled);
        return enabled;
    }

    internal static void SaveLocalBackupEnabled(bool enabled)
    {
        if (enabled && !AppPaths.HasStoragePermission())
            AppPaths.RequestStoragePermission();

        ApplyLocalBackup(enabled);
        SaveBoolean(LocalBackupPreference, enabled);
    }

    internal static bool ReadCloudSyncEnabled()
        => LoadBoolean(CloudSyncPreference, defaultValue: !OperatingSystem.IsAndroid());

    internal static bool LoadAndApplyCloudSyncEnabled()
    {
        var enabled = ReadCloudSyncEnabled();
        ApplyCloudSync(enabled);
        return enabled;
    }

    internal static void SaveCloudSyncEnabled(bool enabled)
    {
        ApplyCloudSync(enabled);
        SaveBoolean(CloudSyncPreference, enabled);
    }

    private static void ApplyLocalBackup(bool enabled)
    {
        CloudSyncCoordinator.SetLocalBackupEnabled(enabled);
        if (enabled)
            AppPaths.EnsureExternalDirectories();
    }

    private static void ApplyCloudSync(bool enabled)
    {
        LauncherCloudSaveState.SetCloudSyncEnabled(enabled);
    }
}
