using System;
using System.Reflection;
using HarmonyLib;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private static void PatchHostService(Harmony harmony, Type hostServiceType, Type patcherType)
    {
        if (hostServiceType == null)
            return;

        var startHostMethod = hostServiceType.GetMethod(
            "StartENetHost",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (startHostMethod != null)
        {
            harmony.Patch(
                startHostMethod,
                postfix: new HarmonyMethod(
                    patcherType.GetMethod(
                        nameof(StartENetHostPostfix),
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                    )
                )
            );
        }

        var disconnectMethod = hostServiceType.GetMethod(
            "Disconnect",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (disconnectMethod != null)
        {
            harmony.Patch(
                disconnectMethod,
                postfix: new HarmonyMethod(
                    patcherType.GetMethod(
                        nameof(DisconnectPostfix),
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                    )
                )
            );
        }
    }
}
