using System;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

internal static partial class UiScalePatches
{
    // Initializes dropdown items with scale percentage text instead of resolution.
    private static bool ResolutionItemInitPrefix(object __instance, Vector2I setResolution)
    {
        if (setResolution.Y != 0)
            return true;

        try
        {
            AccessTools
                .Field(__instance.GetType(), "resolution")
                .SetValue(__instance, setResolution);
            var label = (GodotObject)
                AccessTools.Field(__instance.GetType(), "_label").GetValue(__instance);
            label.Call("SetTextAutoSize", $"{setResolution.X}%");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"ResolutionItemInitPrefix failed: {ex.GetType().Name}: {ex.Message}");
        }
        return false;
    }
}
