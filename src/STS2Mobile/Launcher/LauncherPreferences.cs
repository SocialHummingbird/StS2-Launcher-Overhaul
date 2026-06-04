using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private const string LocalBackupPreferenceKey = "local_backup_enabled";
    private const string CloudSyncPreferenceKey = "cloud_sync_enabled";
    private static readonly BooleanPreference LocalBackupPreference = new(
        LocalBackupPreferenceKey,
        () => false,
        ApplyLocalBackup,
        RequestStoragePermissionForLocalBackup
    );
    private static readonly BooleanPreference CloudSyncPreference = new(
        CloudSyncPreferenceKey,
        () => !OperatingSystem.IsAndroid(),
        ApplyCloudSync
    );

    internal readonly struct ActionPreferences
    {
        internal ActionPreferences(bool localBackupEnabled, bool cloudSyncEnabled)
        {
            LocalBackupEnabled = localBackupEnabled;
            CloudSyncEnabled = cloudSyncEnabled;
        }

        internal bool LocalBackupEnabled { get; }
        internal bool CloudSyncEnabled { get; }
    }

    private readonly struct BooleanPreference
    {
        internal BooleanPreference(
            string key,
            Func<bool> defaultValue,
            Action<bool> apply,
            Action<bool>? beforeSave = null
        )
        {
            Storage = new PreferenceFile(key);
            DefaultValue = defaultValue;
            Apply = apply;
            BeforeSave = beforeSave;
        }

        private PreferenceFile Storage { get; }
        private Func<bool> DefaultValue { get; }
        private Action<bool> Apply { get; }
        private Action<bool>? BeforeSave { get; }

        internal bool Read()
            => Storage.ReadBoolean(DefaultValue());

        internal bool LoadAndApply()
        {
            var enabled = Read();
            Apply(enabled);
            return enabled;
        }

        internal void Save(bool enabled)
        {
            BeforeSave?.Invoke(enabled);
            Apply(enabled);
            Storage.WriteBoolean(enabled);
        }
    }

    internal static void SaveLocalBackupEnabled(bool enabled)
        => LocalBackupPreference.Save(enabled);

    internal static ActionPreferences ReadActionPreferences()
        => CreateActionPreferences(apply: false);

    internal static ActionPreferences LoadAndApplyActionPreferences()
        => CreateActionPreferences(apply: true);

    private static ActionPreferences CreateActionPreferences(bool apply)
        => new(
            apply ? LocalBackupPreference.LoadAndApply() : LocalBackupPreference.Read(),
            apply ? CloudSyncPreference.LoadAndApply() : CloudSyncPreference.Read()
        );

    internal static bool LoadAndApplyCloudSyncEnabled()
        => CloudSyncPreference.LoadAndApply();

    internal static void SaveCloudSyncEnabled(bool enabled)
        => CloudSyncPreference.Save(enabled);

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
