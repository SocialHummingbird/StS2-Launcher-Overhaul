using System;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUI
{
    private static string ResolveLauncherDataDirectory()
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
                PatchHelper.Log($"Launcher internal files dir bridge unavailable: {ex.Message}");
            }

            var androidFilesDir = System.Environment.GetEnvironmentVariable("STS2_ANDROID_FILES_DIR");
            if (BootstrapTrace.TryNormalizeDirectory(androidFilesDir, out var normalizedAndroidFilesDir))
                return normalizedAndroidFilesDir;

            return BootstrapTrace.ResolveFallbackDataDirectory();
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
