using System;
using System.Reflection;
using HarmonyLib;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private static void PatchPlayerNameStrategy(
        Harmony harmony,
        Assembly sts2Asm,
        Type patcherType
    )
    {
        var nullStrategyType = sts2Asm.GetType(
            "MegaCrit.Sts2.Core.Platform.Null.NullPlatformUtilStrategy"
        );
        if (nullStrategyType == null)
            return;

        var getPlayerNameMethod = nullStrategyType.GetMethod(
            "GetPlayerName",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (getPlayerNameMethod == null)
            return;

        harmony.Patch(
            getPlayerNameMethod,
            prefix: new HarmonyMethod(
                patcherType.GetMethod(
                    nameof(GetPlayerNamePrefix),
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                )
            )
        );
    }
}
