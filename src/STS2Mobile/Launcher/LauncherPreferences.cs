using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    private const string LocalBackupPreferenceKey = "local_backup_enabled";
    private const string CloudSyncPreferenceKey = "cloud_sync_enabled";
    private const string GameBranchPreferenceKey = "game_branch";
    private const string RendererModePreferenceKey = "renderer_mode";
    private static readonly PreferenceFile GameBranchPreference = new(GameBranchPreferenceKey);
    private static readonly PreferenceFile RendererModePreference = new(RendererModePreferenceKey);
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
}
