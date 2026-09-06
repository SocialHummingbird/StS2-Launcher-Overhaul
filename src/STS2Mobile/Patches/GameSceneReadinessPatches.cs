using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using STS2Mobile.Launcher;

namespace STS2Mobile.Patches;

internal static class GameSceneReadinessPatches
{
    internal static void Apply(Harmony harmony)
    {
        var menuType = typeof(NGame).Assembly.GetType("MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu")
            ?? throw new InvalidOperationException("Critical readiness patch: NMainMenu not found.");
        // NMainMenu has no _EnterTree override. Capture at construction instead,
        // before any deferred _Ready callback can observe a different attempt.
        var constructor = menuType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null, Type.EmptyTypes, modifiers: null)
            ?? throw new InvalidOperationException("Critical readiness patch: NMainMenu constructor not found.");
        harmony.Patch(constructor, postfix: new HarmonyMethod(PatchHelper.Method(typeof(GameSceneReadinessPatches), nameof(SceneConstructed))));
        PatchHelper.PatchCritical(harmony, menuType, "_Ready",
            postfix: PatchHelper.Method(typeof(GameSceneReadinessPatches), nameof(SceneReady)));
    }

    private static void SceneConstructed(object __instance) => LauncherGameSceneReadiness.BindScene(__instance);
    private static void SceneReady(object __instance) => LauncherGameSceneReadiness.RecordReady(__instance);
}
