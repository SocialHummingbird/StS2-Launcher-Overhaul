using System;
using System.IO;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace STS2Mobile.Patches;

// Applies mobile-friendly default settings on first launch and fixes the VSync
// toggle label bug where the Off and On display values are swapped.
internal static class SettingsPatches
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
    private const string VSyncPaginatorType =
        "MegaCrit.Sts2.Core.Nodes.Screens.Settings.NVSyncPaginator";
    private const string LocStringTypeName = "MegaCrit.Sts2.Core.Localization.LocString";

    private static bool _mobileDefaultsChecked;

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
    }

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

    private static void InitSettingsDataPostfix()
    {
        if (_mobileDefaultsChecked)
            return;
        _mobileDefaultsChecked = true;

        ApplyMobileDefaultsIfNeeded();
    }

    private static void ApplyMobileDefaultsIfNeeded()
    {
        if (File.Exists(MobileDefaultsMarkerPath()))
            return;

        try
        {
            var settings = SaveManager.Instance.SettingsSave;
            settings.VSync = VSyncType.On;
            settings.AspectRatioSetting = AspectRatioSetting.Auto;
            settings.Msaa = 0;
            settings.SkipIntroLogo = true;

            SaveManager.Instance.SaveSettings();

            File.WriteAllText(MobileDefaultsMarkerPath(), MarkerContent);
            PatchHelper.Log(
                "Applied mobile default settings (first launch): VSync=On, AspectRatio=Auto, Msaa=None, SkipIntroLogo=True"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Failed to apply mobile defaults: {ex.Message}");
        }
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
        var getTextMethod = locStringType.GetMethod("GetFormattedText", Type.EmptyTypes);
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

    private static string MobileDefaultsMarkerPath()
        => Path.Combine(OS.GetUserDataDir(), MarkerFileName);

    private static bool SkipIntroLogoPrefix(ref bool __result)
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        __result = true;
        return false;
    }
}
