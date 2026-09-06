using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;
using System;
using System.Threading.Tasks;

namespace STS2Mobile.Patches;

// Installs Harmony hooks for the mobile launcher and Android startup behavior.
internal static class LauncherPatches
{
    internal static void Apply(Harmony harmony)
    {
        ApplyGamePatches(harmony);
        GameSceneReadinessPatches.Apply(harmony);
        ApplySaveManagerPatches(harmony);
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

    private static void ApplySaveManagerPatches(Harmony harmony)
    {
        PatchHelper.PatchCritical(
            harmony,
            typeof(SaveManager),
            "ConstructDefault",
            prefix: PatchHelper.Method(
                typeof(LauncherPatches),
                nameof(ConstructDefault)
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
        if (!OperatingSystem.IsAndroid())
            return true;

        PatchHelper.Log("[Save] Constructing Android gameplay SaveManager");
        __result = new SaveManager(
            new AndroidLocalSaveStore(
                SaveSyncService.NotifyGameplayMutationCommitted
            )
        );
        return false;
    }
}
