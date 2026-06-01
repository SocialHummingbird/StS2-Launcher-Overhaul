using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;
using System;
using System.Threading.Tasks;

namespace STS2Mobile.Patches;

// Installs Harmony hooks for the mobile launcher, cloud-save bridge, and Android startup behavior.
internal static class LauncherPatches
{
    internal static void Apply(Harmony harmony)
    {
        ApplyGamePatches(harmony);
        ApplyCloudSavePatches(harmony);
    }

    private static void ApplyGamePatches(Harmony harmony)
    {
        PatchHelper.PatchCritical(
            harmony,
            typeof(NGame),
            "GameStartupWrapper",
            prefix: PatchHelper.Method(
                typeof(LauncherPatches),
                nameof(GameStartupWrapper)
            )
        );

        PatchHelper.PatchGetter(
            harmony,
            typeof(NGame),
            "StartOnMainMenu",
            prefix: PatchHelper.Method(
                typeof(LauncherPatches),
                nameof(StartOnMainMenu)
            )
        );
    }

    private static void ApplyCloudSavePatches(Harmony harmony)
    {
        PatchHelper.Patch(
            harmony,
            typeof(SaveManager),
            "ConstructDefault",
            prefix: PatchHelper.Method(
                typeof(LauncherPatches),
                nameof(ConstructDefault)
            )
        );

        PatchHelper.PatchCritical(
            harmony,
            typeof(CloudSaveStore),
            "SyncCloudToLocal",
            prefix: PatchHelper.Method(
                typeof(LauncherPatches),
                nameof(SyncCloudToLocal)
            )
        );

        PatchHelper.PatchCritical(
            harmony,
            typeof(SaveManager),
            "TryFirstTimeCloudSync",
            prefix: PatchHelper.Method(
                typeof(LauncherPatches),
                nameof(TryFirstTimeCloudSync)
            )
        );

        PatchHelper.PatchCritical(
            harmony,
            typeof(SaveManager),
            "SyncCloudToLocal",
            prefix: PatchHelper.Method(
                typeof(LauncherPatches),
                nameof(SaveManagerSyncCloudToLocal)
            )
        );
    }

    private static bool GameStartupWrapper(object __instance, ref Task __result)
    {
        __result = LauncherStartupFlow.RunAsync(__instance);
        return false;
    }

    private static bool StartOnMainMenu(ref bool __result)
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        __result = true;
        return false;
    }

    private static bool ConstructDefault(ref SaveManager __result)
    {
        PatchHelper.Log($"[Cloud] ConstructDefaultPrefix called. {LauncherCloudSaveState.StatusSummary}");

        if (!LauncherCloudSaveState.TryCreateEnabledSaveManager(out var saveManager))
            return true;

        __result = saveManager;
        return false;
    }

    private static bool SyncCloudToLocal(
        CloudSaveStore __instance,
        string path,
        ref Task __result
    )
    {
        __result = CloudSyncCoordinator.AutoSyncFileAsync(
            __instance.LocalStore,
            __instance.CloudStore,
            path
        );
        return false;
    }

    private static bool TryFirstTimeCloudSync(ref Task<bool> __result)
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        __result = Task.FromResult(false);
        PatchHelper.Log("[Cloud] Skipping upstream first-time cloud sync on Android");
        return false;
    }

    private static bool SaveManagerSyncCloudToLocal(ref Task __result)
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        __result = Task.CompletedTask;
        PatchHelper.Log("[Cloud] Skipping upstream startup cloud sync on Android");
        return false;
    }

}
