using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;

namespace STS2Mobile.Patches;

// Stage 3 diagnostic only. Both experiment arms use the same APK. Android
// snapshots the ADB-controlled setting in Activity.onCreate, so the selected
// arm cannot change during a process. With no setting, behavior is unchanged.
internal static class DeferredPreloadExperimentPatches
{
    private static bool _suppressFirstCall;
    private static int _commonAndMainMenuCallCount;
    private static int _suppressionState;

    internal static void Apply(Harmony harmony)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        _suppressFirstCall = ReadSuppressionArm();
        _commonAndMainMenuCallCount = 0;
        _suppressionState = 0;

        var executeDeferredPatched = PatchExact(
            harmony,
            typeof(OneTimeInitialization),
            nameof(OneTimeInitialization.ExecuteDeferred),
            prefix: null,
            postfix: nameof(ExecuteDeferredPostfix),
            expectedReturnType: typeof(void)
        );
        var commonAndMainMenuPatched = PatchExact(
            harmony,
            typeof(PreloadManager),
            nameof(PreloadManager.LoadCommonAndMainMenuAssets),
            prefix: nameof(LoadCommonAndMainMenuAssetsPrefix),
            postfix: null,
            expectedReturnType: typeof(Task)
        );

        if (executeDeferredPatched && commonAndMainMenuPatched)
        {
            PatchHelper.Log(
                "[DeferredPreloadExperiment] installed arm="
                + (_suppressFirstCall ? "suppress-first" : "normal")
            );
        }
        else
        {
            PatchHelper.Log(
                "[DeferredPreloadExperiment] unavailable; normal loading remains active"
            );
        }
    }

    private static bool ReadSuppressionArm()
    {
        try
        {
            return AndroidGodotAppBridge.IsDeferredPreloadExperimentEnabled();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                "[DeferredPreloadExperiment] setting bridge read failed; using normal loading: "
                + ex.Message
            );
            return false;
        }
    }

    private static void ExecuteDeferredPostfix()
    {
        if (_suppressFirstCall
            && Interlocked.CompareExchange(ref _suppressionState, 1, 0) == 0)
            PatchHelper.Log("[DeferredPreloadExperiment] suppression armed after ExecuteDeferred");

        PatchHelper.Log("[DeferredPreloadExperiment] ExecuteDeferred completed");
    }

    private static bool LoadCommonAndMainMenuAssetsPrefix(ref Task __result)
    {
        var call = Interlocked.Increment(ref _commonAndMainMenuCallCount);
        if (Interlocked.CompareExchange(ref _suppressionState, 2, 1) == 1)
        {
            __result = Task.CompletedTask;
            PatchHelper.Log(
                $"[DeferredPreloadExperiment] LoadCommonAndMainMenuAssets call={call} action=suppressed"
            );
            return false;
        }

        PatchHelper.Log(
            $"[DeferredPreloadExperiment] LoadCommonAndMainMenuAssets call={call} action=normal"
        );
        return true;
    }

    private static bool PatchExact(
        Harmony harmony,
        Type declaringType,
        string methodName,
        string prefix,
        string postfix,
        Type expectedReturnType
    )
    {
        var target = declaringType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null
        );
        if (target == null || target.ReturnType != expectedReturnType)
        {
            PatchHelper.Log(
                $"FAILED {declaringType.FullName}.{methodName}: exact zero-parameter "
                + $"{expectedReturnType.Name} signature not found"
            );
            return false;
        }

        harmony.Patch(
            target,
            prefix: prefix == null
                ? null
                : new HarmonyMethod(
                    PatchHelper.Method(typeof(DeferredPreloadExperimentPatches), prefix)
                ),
            postfix: postfix == null
                ? null
                : new HarmonyMethod(
                    PatchHelper.Method(typeof(DeferredPreloadExperimentPatches), postfix)
                )
        );
        PatchHelper.Log($"Patched {declaringType.FullName}.{methodName}");
        return true;
    }
}
