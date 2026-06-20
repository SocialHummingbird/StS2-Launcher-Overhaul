using System;
using System.IO;
using Godot;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace STS2Mobile.Patches;

internal static partial class SettingsPatches
{
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
