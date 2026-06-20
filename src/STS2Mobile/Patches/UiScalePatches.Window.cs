using System;
using System.Reflection;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

internal static partial class UiScalePatches
{
    private static void PatchWindowChangeHandlers(Harmony harmony, Assembly sts2Asm)
    {
        var globalUiType = sts2Asm.GetType("MegaCrit.Sts2.Core.Nodes.CommonUi.NGlobalUi");
        if (globalUiType != null)
        {
            PatchHelper.Patch(
                harmony,
                globalUiType,
                "OnWindowChange",
                prefix: PatchHelper.Method(typeof(UiScalePatches), nameof(GlobalUiWindowChangePrefix))
            );
        }

        var mainMenuType = sts2Asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu");
        if (mainMenuType != null)
        {
            PatchHelper.Patch(
                harmony,
                mainMenuType,
                "OnWindowChange",
                prefix: PatchHelper.Method(typeof(UiScalePatches), nameof(MainMenuWindowChangePrefix))
            );
        }
    }

    // Reapplies the scaled content size when the window changes (e.g. rotation).
    private static bool GlobalUiWindowChangePrefix(object __instance)
    {
        if (
            MegaCrit.Sts2.Core.Saves.SaveManager.Instance.SettingsSave.AspectRatioSetting
            != MegaCrit.Sts2.Core.Settings.AspectRatioSetting.Auto
        )
            return true; // let the original handle non-Auto settings

        ApplyWindowScaleFromField(__instance, nameof(GlobalUiWindowChangePrefix));
        return false;
    }

    // Handles window change on the main menu screen specifically.
    private static bool MainMenuWindowChangePrefix(object __instance, bool isAspectRatioAuto)
    {
        if (!isAspectRatioAuto)
            return false;

        ApplyWindowScaleFromField(__instance, nameof(MainMenuWindowChangePrefix));
        return false;
    }

    private static void ApplyWindowScaleFromField(object instance, string context)
    {
        EnsureUiScaleLoaded();
        try
        {
            var window = (Window)
                AccessTools.Field(instance.GetType(), "_window").GetValue(instance);
            ApplyScaledContentSize(window);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"{context} failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
