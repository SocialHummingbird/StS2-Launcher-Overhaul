using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Patches;

// Upstream SteamControllerInputStrategy probes NControllerManager.Instance before
// checking whether Steamworks exists. On Android that static path can be null
// during early startup, which turns normal fallback controller initialization
// into an unobserved async NullReferenceException.
internal static class ControllerInputPatches
{
    private const BindingFlags AllFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private const string FallbackStrategyFieldName = "_fallbackStrategy";
    private static bool _loggedFallbackInit;

    internal static void Apply(Harmony harmony)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        PatchNInputManagerInit(harmony);
        PatchSteamControllerInputInit(harmony);
    }

    private static void PatchSteamControllerInputInit(Harmony harmony)
    {
        var target = typeof(SteamControllerInputStrategy).GetMethod("Init", AllFlags);
        var prefix = typeof(ControllerInputPatches).GetMethod(
            nameof(SteamControllerInputInitPrefix),
            AllFlags
        );

        if (target == null || prefix == null)
        {
            PatchHelper.Log("Controller input patch skipped: SteamControllerInputStrategy.Init not found");
            return;
        }

        harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        PatchHelper.Log("Patched SteamControllerInputStrategy.Init Android fallback");
    }

    private static void PatchNInputManagerInit(Harmony harmony)
    {
        var target = typeof(NInputManager).GetMethod("Init", AllFlags);
        var prefix = typeof(ControllerInputPatches).GetMethod(
            nameof(NInputManagerInitPrefix),
            AllFlags
        );

        if (target == null || prefix == null)
        {
            PatchHelper.Log("Controller input patch skipped: NInputManager.Init not found");
            return;
        }

        harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        PatchHelper.Log("Patched NInputManager.Init Android settings readiness");
    }

    private static bool SteamControllerInputInitPrefix(
        SteamControllerInputStrategy __instance,
        ref Task __result
    )
    {
        var fallback = GetFallbackStrategy(__instance);
        if (fallback == null)
        {
            PatchHelper.Log("Controller input patch skipped: fallback strategy field unavailable");
            return true;
        }

        __result = InitAndroidFallback(fallback);
        return false;
    }

    private static IControllerInputStrategy GetFallbackStrategy(
        SteamControllerInputStrategy instance
    )
    {
        var field = typeof(SteamControllerInputStrategy).GetField(FallbackStrategyFieldName, AllFlags);
        return field?.GetValue(instance) as IControllerInputStrategy;
    }

    private static async Task InitAndroidFallback(IControllerInputStrategy fallback)
    {
        await fallback.Init();

        if (_loggedFallbackInit)
            return;

        _loggedFallbackInit = true;
        PatchHelper.Log("Controller input patch initialized Godot fallback strategy on Android");
    }

    private static bool NInputManagerInitPrefix(ref Task __result)
    {
        try
        {
            // Input nodes can become ready before the launcher's normal settings stage.
            // Prepare the real settings, then leave bindings to this game build's Init.
            var saves = SaveManager.Instance;
            if (saves.SettingsSave == null)
            {
                PatchHelper.Log("Controller input patch: SettingsSave was null; initializing settings data");
                saves.InitSettingsData();
            }

            return true;
        }
        catch (Exception ex)
        {
            __result = Task.FromException(ex);
            return false;
        }
    }
}
