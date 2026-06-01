using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static void ResetSaveManagerInstance()
    {
        var instanceField = typeof(SaveManager).GetField(
            "_instance",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        if (instanceField == null)
            return;

        instanceField.SetValue(null, null);
        PatchHelper.Log("[Cloud] Reset SaveManager._instance for cloud store re-injection");
    }

    private static bool InitializeSettingsAndSaves(StartupContext startup)
    {
        startup.SetPhase(
            PhaseSettingsAndSaves,
            startup.ForceLocalSaves
                ? "Loading settings and saves in local-only safe mode..."
                : "Loading settings and saves..."
        );
        PatchHelper.Log("Initializing settings and save manager");
        try
        {
            SaveManager.Instance.InitSettingsData();
            return true;
        }
        catch (Exception ex)
        {
            startup.HandleSettingsAndSavesFailure(ex);
            return false;
        }
    }
}
