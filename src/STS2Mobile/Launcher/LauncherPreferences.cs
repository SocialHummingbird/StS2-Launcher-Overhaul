using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private const string LocalBackupPreference = "local_backup_enabled";
    private const string CloudSyncPreference = "cloud_sync_enabled";

    private readonly struct BooleanPreference
    {
        internal BooleanPreference(
            string key,
            Func<bool> defaultValue,
            Action<bool> apply,
            Action<bool>? beforeSave = null
        )
        {
            Key = key;
            DefaultValue = defaultValue;
            Apply = apply;
            BeforeSave = beforeSave;
        }

        internal string Key { get; }
        internal Func<bool> DefaultValue { get; }
        internal Action<bool> Apply { get; }
        internal Action<bool>? BeforeSave { get; }
    }

    internal static bool ReadLocalBackupEnabled()
        => ReadBooleanPreference(LocalBackup());

    internal static bool LoadAndApplyLocalBackupEnabled()
        => LoadAndApplyBooleanPreference(LocalBackup());

    internal static void SaveLocalBackupEnabled(bool enabled)
        => SaveBooleanPreference(LocalBackup(), enabled);

    internal static bool ReadCloudSyncEnabled()
        => ReadBooleanPreference(CloudSync());

    internal static bool LoadAndApplyCloudSyncEnabled()
        => LoadAndApplyBooleanPreference(CloudSync());

    internal static void SaveCloudSyncEnabled(bool enabled)
        => SaveBooleanPreference(CloudSync(), enabled);

    private static BooleanPreference LocalBackup()
        => new(
            LocalBackupPreference,
            () => false,
            ApplyLocalBackup,
            RequestStoragePermissionForLocalBackup
        );

    private static BooleanPreference CloudSync()
        => new(
            CloudSyncPreference,
            () => !OperatingSystem.IsAndroid(),
            ApplyCloudSync
        );

    private static bool ReadBooleanPreference(BooleanPreference preference)
        => LoadBoolean(preference.Key, preference.DefaultValue());

    private static bool LoadAndApplyBooleanPreference(BooleanPreference preference)
    {
        var enabled = ReadBooleanPreference(preference);
        preference.Apply(enabled);
        return enabled;
    }

    private static void SaveBooleanPreference(BooleanPreference preference, bool enabled)
    {
        preference.BeforeSave?.Invoke(enabled);
        preference.Apply(enabled);
        SaveBoolean(preference.Key, enabled);
    }

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

    private static void ApplyCloudSync(bool enabled)
    {
        LauncherCloudSaveState.SetCloudSyncEnabled(enabled);
    }
}
