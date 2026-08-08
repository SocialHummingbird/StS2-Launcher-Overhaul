using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2Mobile.Patches;

internal static partial class SettingsPatches
{
    private static void PatchVSyncString(Harmony harmony)
    {
        var vsyncPaginatorType = typeof(NGame).Assembly.GetType(VSyncPaginatorType);
        if (vsyncPaginatorType == null)
            return;

        PatchHelper.Patch(
            harmony,
            vsyncPaginatorType,
            GetVSyncStringMethod,
            prefix: PatchHelper.Method(typeof(SettingsPatches), nameof(GetVSyncStringPrefix))
        );
    }

    private static bool GetVSyncStringPrefix(object vsyncType, ref string __result)
    {
        try
        {
            __result = GetVSyncText(vsyncType);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"GetVSyncStringPrefix failed: {ex.Message}");
            __result = "On";
        }
        return false;
    }

    private static string GetVSyncText(object vsyncType)
    {
        var locStringType = typeof(NGame).Assembly.GetType(LocStringTypeName);
        var ctor = locStringType.GetConstructor(new[] { typeof(string), typeof(string) });
        var locString = ctor.Invoke(new object[] { SettingsTable, VSyncKeyFor(vsyncType) });
        var getTextMethod = locStringType.GetMethod(GetFormattedTextMethod, Type.EmptyTypes);
        return (string)getTextMethod.Invoke(locString, null);
    }

    private static string VSyncKeyFor(object vsyncType)
    {
        return (int)vsyncType switch
        {
            1 => VSyncOffKey,
            2 => VSyncOnKey,
            3 => VSyncAdaptiveKey,
            _ => VSyncAdaptiveKey,
        };
    }
}
