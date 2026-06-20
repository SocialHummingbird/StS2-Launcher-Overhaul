using System;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

internal static partial class UiScalePatches
{
    // Always enable the dropdown since mobile has no windowed/fullscreen toggle.
    private static bool RefreshEnabledPrefix(object __instance)
    {
        try
        {
            var enableMethod = AccessTools.Method(__instance.GetType(), "Enable");
            enableMethod?.Invoke(__instance, null);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"RefreshEnabledPrefix failed: {ex.GetType().Name}: {ex.Message}");
        }
        return false;
    }

    // Replaces resolution entries with scale percentage options (100% to 150%).
    private static bool PopulateScaleItemsPrefix(object __instance)
    {
        try
        {
            var instType = __instance.GetType();
            AccessTools.Method(instType, "ClearDropdownItems").Invoke(__instance, null);

            var dropdownItems = (Node)
                AccessTools.Field(instType, "_dropdownItems").GetValue(__instance);
            var scene = (PackedScene)
                AccessTools.Field(instType, "_dropdownItemScene").GetValue(__instance);

            int[] scales = { 100, 110, 120, 130, 140, 150 };
            foreach (int scale in scales)
            {
                var item = scene.Instantiate(PackedScene.GenEditState.Disabled);
                dropdownItems.AddChild(item);
                item.Connect(
                    "Selected",
                    new Callable((GodotObject)__instance, "OnDropdownItemSelected")
                );
                item.Call("Init", new Vector2I(scale, 0));
            }

            dropdownItems.GetParent().Call("RefreshLayout");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"PopulateScaleItems failed: {ex.Message}");
        }
        return false;
    }

    // Shows the current scale percentage in the dropdown label.
    private static bool RefreshScaleLabelPrefix(object __instance)
    {
        EnsureUiScaleLoaded();
        try
        {
            var label = (GodotObject)
                AccessTools.Field(__instance.GetType(), "_currentOptionLabel").GetValue(__instance);
            label.Call("SetTextAutoSize", $"{UiScalePercent}%");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"RefreshScaleLabelPrefix failed: {ex.GetType().Name}: {ex.Message}");
        }
        return false;
    }

    // Applies the selected scale, saves it, and updates the label.
    private static bool ScaleItemSelectedPrefix(object __instance, object nDropdownItem)
    {
        try
        {
            var resField = AccessTools.Field(nDropdownItem.GetType(), "resolution");
            var resolution = (Vector2I)resField.GetValue(nDropdownItem);
            if (resolution.Y != 0)
                return true;

            int newScale = resolution.X;
            if (newScale == UiScalePercent)
                return false;

            AccessTools.Method(__instance.GetType(), "CloseDropdown").Invoke(__instance, null);

            UiScalePercent = newScale;
            SaveUiScale();
            ApplyUiScale();

            var label = (GodotObject)
                AccessTools.Field(__instance.GetType(), "_currentOptionLabel").GetValue(__instance);
            label.Call("SetTextAutoSize", $"{UiScalePercent}%");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"ScaleItemSelected failed: {ex.Message}");
        }
        return false;
    }
}
