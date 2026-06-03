using System;
using System.IO;

namespace STS2Mobile;

// Shared path constants for external storage directories and permission helpers.
internal static class AppPaths
{
    private const string ExternalStorageRoot = "/storage/emulated/0/StS2Launcher";

    internal const string ExternalModsDir = ExternalStorageRoot + "/Mods";
    internal const string ExternalSaveBackupsDir = ExternalStorageRoot + "/Saves";

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

    private static void EnsureExternalDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Storage] Failed to create external directory {path}: {ex.Message}");
        }
    }
}
