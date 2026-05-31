using System;
using System.IO;
using Godot;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherPreferences
{
    private const string LocalBackupPreference = "local_backup_enabled";
    private const string CloudSyncPreference = "cloud_sync_enabled";
    private const string TrueValue = "true";
    private const string FalseValue = "false";

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

    private static bool LoadBoolean(string fileName, bool defaultValue)
    {
        try
        {
            var path = Path.Combine(OS.GetDataDir(), fileName);
            if (File.Exists(path))
                return File.ReadAllText(path).Trim() == TrueValue;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Preference load failed for {fileName}: {ex.Message}");
        }

        return defaultValue;
    }

    private static void SaveBoolean(string fileName, bool enabled)
    {
        try
        {
            var path = Path.Combine(OS.GetDataDir(), fileName);
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(parent))
                Directory.CreateDirectory(parent);

            File.WriteAllText(path, enabled ? TrueValue : FalseValue);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Preference save failed for {fileName}: {ex.Message}");
        }
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
