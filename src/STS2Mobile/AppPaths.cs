using System;
using System.IO;
using Godot;

namespace STS2Mobile;

// Shared path constants for external storage directories and permission helpers.
internal static class AppPaths
{
    private const string ExternalStorageRoot = "/storage/emulated/0/StS2Launcher";
    private const string AndroidFilesDirEnvironmentVariable = "STS2_ANDROID_FILES_DIR";
    private const string WorkshopModsDirectoryName = "workshop_mods";
    private const string WorkshopDownloadsDirectoryName = "downloads";
    private const string WorkshopStagedDirectoryName = "staged";
    private const string WorkshopManifestFileName = "workshop_sync_manifest.json";
    private const string WorkshopClearMarkerFileName = "last_workshop_mod_clear.txt";
    private const string WorkshopConsentMarkerFileName = "workshop_mods_accepted.txt";
    private const string ModsDirectoryName = "mods";
    private const string ModSelectionFileName = "mod_selection.json";
    private const string LastModLaunchFileName = "last_mod_launch.json";

    internal const string ExternalModsDir = ExternalStorageRoot + "/Mods";
    internal const string ExternalSaveBackupsDir = ExternalStorageRoot + "/Saves";
    internal static string AppPrivateWorkshopDownloadsDir =>
        WorkshopDownloadsDir(AppPrivateDataDir);
    internal static string AppPrivateWorkshopStagedModsDir =>
        WorkshopStagedModsDir(AppPrivateDataDir);
    internal static string AppPrivateWorkshopManifestPath =>
        WorkshopManifestPath(AppPrivateDataDir);
    internal static string AppPrivateWorkshopClearMarkerPath =>
        WorkshopClearMarkerPath(AppPrivateDataDir);
    internal static string AppPrivateWorkshopConsentMarkerPath =>
        WorkshopConsentMarkerPath(AppPrivateDataDir);
    internal static string AppPrivateModSelectionPath =>
        ModSelectionPath(AppPrivateDataDir);
    internal static string AppPrivateLastModLaunchPath =>
        LastModLaunchPath(AppPrivateDataDir);

    internal static string WorkshopDownloadsDir(string dataDir) =>
        Path.Combine(dataDir, WorkshopModsDirectoryName, WorkshopDownloadsDirectoryName);

    internal static string WorkshopStagedModsDir(string dataDir) =>
        Path.Combine(dataDir, WorkshopModsDirectoryName, WorkshopStagedDirectoryName);

    internal static string WorkshopManifestPath(string dataDir) =>
        Path.Combine(dataDir, WorkshopModsDirectoryName, WorkshopManifestFileName);

    internal static string WorkshopClearMarkerPath(string dataDir) =>
        Path.Combine(dataDir, WorkshopModsDirectoryName, WorkshopClearMarkerFileName);

    internal static string WorkshopConsentMarkerPath(string dataDir) =>
        Path.Combine(dataDir, WorkshopModsDirectoryName, WorkshopConsentMarkerFileName);

    internal static string ModSelectionPath(string dataDir) =>
        Path.Combine(dataDir, ModsDirectoryName, ModSelectionFileName);

    internal static string LastModLaunchPath(string dataDir) =>
        Path.Combine(dataDir, ModsDirectoryName, LastModLaunchFileName);

    private static string AppPrivateDataDir => ResolveAppPrivateDataDirectory();

    // Returns true if the app has permission to write to shared external storage.
    internal static bool HasStoragePermission()
    {
        try
        {
            return AndroidGodotAppBridge.HasStoragePermission();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to check storage permission: {ex.Message}");
            return false;
        }
    }

    // Requests external storage permission. On Android 11+, opens the system
    // settings page. On older versions, shows the runtime permission dialog.
    internal static void RequestStoragePermission()
    {
        try
        {
            AndroidGodotAppBridge.RequestStoragePermission();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to request storage permission: {ex.Message}");
        }
    }

    // Creates the external Mods and Saves directories if storage permission is granted.
    internal static void EnsureExternalDirectories()
    {
        if (!HasStoragePermission())
            return;

        EnsureExternalDirectory(ExternalModsDir);
        EnsureExternalDirectory(ExternalSaveBackupsDir);
    }

    internal static void EnsureWorkshopDirectories()
    {
        EnsureDirectory(AppPrivateWorkshopDownloadsDir, "Workshop downloads");
        EnsureDirectory(AppPrivateWorkshopStagedModsDir, "Workshop staged mods");
        EnsureDirectory(Path.GetDirectoryName(AppPrivateModSelectionPath), "mod selection");
    }

    private static void EnsureExternalDirectory(string path)
        => EnsureDirectory(path, "external storage");

    private static void EnsureDirectory(string path, string label)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Storage] Failed to create {label} directory {path}: {ex.Message}");
        }
    }

    private static string ResolveAppPrivateDataDirectory()
    {
        if (OperatingSystem.IsAndroid())
        {
            try
            {
                var bridgedFilesDir = AndroidGodotAppBridge.GetInternalFilesDirPath();
                if (BootstrapTrace.TryNormalizeDirectory(bridgedFilesDir, out var normalizedBridgedFilesDir))
                    return normalizedBridgedFilesDir;
            }
            catch (Exception ex)
            {
                PatchHelper.Log($"[Storage] Android internal files bridge unavailable: {ex.Message}");
            }

            var androidFilesDir = System.Environment.GetEnvironmentVariable(AndroidFilesDirEnvironmentVariable);
            if (BootstrapTrace.TryNormalizeDirectory(androidFilesDir, out var normalizedAndroidFilesDir))
                return normalizedAndroidFilesDir;
        }

        try
        {
            var dataDir = OS.GetDataDir();
            if (BootstrapTrace.TryNormalizeDirectory(dataDir, out var normalized))
                return normalized;
        }
        catch
        {
        }

        return BootstrapTrace.ResolveFallbackDataDirectory();
    }
}
