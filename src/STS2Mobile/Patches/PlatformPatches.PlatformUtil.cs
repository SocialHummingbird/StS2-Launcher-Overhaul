using System;
using HarmonyLib;

namespace STS2Mobile.Patches;

internal static partial class PlatformPatches
{
    private static void ApplyPlatformUtilPatches(Harmony harmony)
    {
        try
        {
            PatchPlatformUtilStaticConstructor(harmony);
            PatchPlatformUtilPrimaryPlatformGetter(harmony);
            PatchGetPlatformUtil(harmony);
            PatchPlatformUtilMethods(harmony);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PlatformUtil patch failed: {ex.Message}");
            throw;
        }
    }
}
