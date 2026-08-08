using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace STS2Mobile.Patches;

// Applies mobile-friendly default settings on first launch and fixes the VSync
// toggle label bug where the Off and On display values are swapped.
internal static partial class SettingsPatches
{
    private const string GetVSyncStringMethod = "GetVSyncString";
    private const string InitSettingsDataMethod = "InitSettingsData";
    private const string MarkerContent = "1";
    private const string MarkerFileName = ".mobile_defaults_applied";
    private const string SkipIntroLogoProperty = "SkipIntroLogo";
    private const string SettingsTable = "settings_ui";
    private const string VSyncAdaptiveKey = "VSYNC_ADAPTIVE";
    private const string VSyncOffKey = "VSYNC_OFF";
    private const string VSyncOnKey = "VSYNC_ON";
    private const string GetFormattedTextMethod = "GetFormattedText";
    private const string VSyncPaginatorType =
        "MegaCrit.Sts2.Core.Nodes.Screens.Settings.NVSyncPaginator";
    private const string LocStringTypeName = "MegaCrit.Sts2.Core.Localization.LocString";

    private static bool _mobileDefaultsChecked;
    private static int _locStringFallbackLogged;

    internal static void Apply(Harmony harmony)
    {
        // Apply mobile defaults on first launch; user preferences are respected after that.
        PatchHelper.Patch(
            harmony,
            typeof(SaveManager),
            InitSettingsDataMethod,
            postfix: PatchHelper.Method(typeof(SettingsPatches), nameof(InitSettingsDataPostfix))
        );

        PatchHelper.PatchGetter(
            harmony,
            typeof(SettingsSave),
            SkipIntroLogoProperty,
            prefix: PatchHelper.Method(typeof(SettingsPatches), nameof(SkipIntroLogoPrefix))
        );

        PatchVSyncString(harmony);
        PatchLocStringGetFormattedText(harmony);
    }
}
