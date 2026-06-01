using System;
using STS2Mobile;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal partial class LauncherModel
{
    private bool TrySafeAndroidRestart()
    {
        if (!OperatingSystem.IsAndroid() || !LauncherGameFiles.Ready(_dataDir))
            return false;

        PatchHelper.Log(RestartMessage(safe: true));
        AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
        return true;
    }

    private void RestartForLaunch(bool safe)
    {
        PatchHelper.Log(RestartMessage(safe));

        if (!LauncherGameFiles.Ready(_dataDir))
        {
            AndroidGodotAppBridge.RestartApp();
            return;
        }

        if (safe)
            AndroidGodotAppBridge.LaunchGameSafelyOnRestart();
        else
            AndroidGodotAppBridge.LaunchGameOnRestart();
    }

    private static string RestartMessage(bool safe)
        => safe
            ? "[Launcher] Restarting app for safe game launch"
            : "[Launcher] Restarting app to launch game files";
}
