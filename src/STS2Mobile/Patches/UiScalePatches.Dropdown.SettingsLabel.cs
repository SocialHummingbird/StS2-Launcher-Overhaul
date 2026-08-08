using System;
using Godot;

namespace STS2Mobile.Patches;

internal static partial class UiScalePatches
{
    // Renames the "Resolution" label to "UI Scale" in the settings screen.
    private static void LocalizeLabelsPostfix(object __instance)
    {
        try
        {
            var screen = (Node)__instance;
            var graphicsPanel = screen.GetNode("%GraphicsSettings");
            var content = (Node)((GodotObject)graphicsPanel).Get("Content");
            var resNode = content.GetNode("WindowedResolution");
            var label = (GodotObject)resNode.GetNode("Label");
            label.Set("text", "UI Scale");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"LocalizeLabels postfix failed: {ex.Message}");
        }
    }
}
