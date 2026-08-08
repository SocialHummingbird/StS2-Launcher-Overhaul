using System.Reflection;
using HarmonyLib;

namespace STS2Mobile.Patches;

internal static partial class UiScalePatches
{
    private static void PatchResolutionDropdown(Harmony harmony, Assembly sts2Asm)
    {
        var resDropdownType = sts2Asm.GetType(
            "MegaCrit.Sts2.Core.Nodes.Screens.Settings.NResolutionDropdown"
        );
        if (resDropdownType == null)
            return;

        PatchHelper.Patch(
            harmony,
            resDropdownType,
            "RefreshEnabled",
            prefix: PatchHelper.Method(typeof(UiScalePatches), nameof(RefreshEnabledPrefix))
        );

        PatchHelper.Patch(
            harmony,
            resDropdownType,
            "PopulateDropdownItems",
            prefix: PatchHelper.Method(typeof(UiScalePatches), nameof(PopulateScaleItemsPrefix))
        );

        PatchHelper.Patch(
            harmony,
            resDropdownType,
            "RefreshCurrentlySelectedResolution",
            prefix: PatchHelper.Method(typeof(UiScalePatches), nameof(RefreshScaleLabelPrefix))
        );

        PatchHelper.Patch(
            harmony,
            resDropdownType,
            "OnDropdownItemSelected",
            prefix: PatchHelper.Method(typeof(UiScalePatches), nameof(ScaleItemSelectedPrefix))
        );
    }

    private static void PatchResolutionDropdownItem(Harmony harmony, Assembly sts2Asm)
    {
        var resItemType = sts2Asm.GetType(
            "MegaCrit.Sts2.Core.Nodes.Screens.Settings.NResolutionDropdownItem"
        );
        if (resItemType == null)
            return;

        PatchHelper.Patch(
            harmony,
            resItemType,
            "Init",
            prefix: PatchHelper.Method(typeof(UiScalePatches), nameof(ResolutionItemInitPrefix))
        );
    }

    private static void PatchSettingsScreen(Harmony harmony, Assembly sts2Asm)
    {
        var settingsScreenType = sts2Asm.GetType(
            "MegaCrit.Sts2.Core.Nodes.Screens.Settings.NSettingsScreen"
        );
        if (settingsScreenType == null)
            return;

        PatchHelper.Patch(
            harmony,
            settingsScreenType,
            "LocalizeLabels",
            postfix: PatchHelper.Method(typeof(UiScalePatches), nameof(LocalizeLabelsPostfix))
        );
    }
}
