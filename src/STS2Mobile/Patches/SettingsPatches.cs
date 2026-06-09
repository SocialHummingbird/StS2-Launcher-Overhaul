using System;
using System.IO;
using System.Reflection;
using System.Threading;
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

    private static void PatchLocStringGetFormattedText(Harmony harmony)
    {
        try
        {
            var locStringType = typeof(NGame).Assembly.GetType(LocStringTypeName);
            var getFormattedText = locStringType?.GetMethod(
                GetFormattedTextMethod,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (getFormattedText == null)
            {
                PatchHelper.Log("LocString.GetFormattedText patch skipped");
                return;
            }

            harmony.Patch(
                getFormattedText,
                finalizer: new HarmonyMethod(
                    PatchHelper.Method(typeof(SettingsPatches), nameof(GetFormattedTextFinalizer))
                )
            );
            PatchHelper.Log("Patched LocString.GetFormattedText finalizer");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"LocString.GetFormattedText patch failed: {ex.Message}");
        }
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

    private static string MobileDefaultsMarkerPath()
        => Path.Combine(OS.GetUserDataDir(), MarkerFileName);

    private static bool SkipIntroLogoPrefix(ref bool __result)
    {
        if (!OperatingSystem.IsAndroid())
            return true;

        __result = true;
        return false;
    }

    private static Exception GetFormattedTextFinalizer(
        object __instance,
        ref string __result,
        Exception __exception
    )
    {
        if (__exception == null || !OperatingSystem.IsAndroid())
            return __exception;

        if (__exception is not NullReferenceException)
            return __exception;

        __result = GetLocStringFallback(__instance);
        if (Interlocked.Exchange(ref _locStringFallbackLogged, 1) == 0)
            PatchHelper.Log(
                $"LocString.GetFormattedText null fallback active; first fallback='{__result}'"
            );

        return null;
    }

    private static string GetLocStringFallback(object locString)
    {
        if (locString == null)
            return string.Empty;

        var table = ReadStringMember(locString, "Table", "TableName", "_table", "table");
        var key = ReadStringMember(locString, "Key", "LocKey", "_key", "key", "Id", "id");

        if (!string.IsNullOrWhiteSpace(key))
            return key;
        if (!string.IsNullOrWhiteSpace(table))
            return table;

        foreach (var field in locString.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (field.FieldType != typeof(string))
                continue;

            if (field.GetValue(locString) is string value && !string.IsNullOrWhiteSpace(value))
                return value;
        }

        return string.Empty;
    }

    private static string ReadStringMember(object instance, params string[] names)
    {
        var type = instance.GetType();
        foreach (var name in names)
        {
            var property = type.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (property?.GetValue(instance) is string propertyValue)
                return propertyValue;

            var field = type.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (field?.GetValue(instance) is string fieldValue)
                return fieldValue;
        }

        return string.Empty;
    }
}
