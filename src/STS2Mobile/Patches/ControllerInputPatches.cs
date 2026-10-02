using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.ControllerInput.ControllerConfigs;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Patches;

// Upstream SteamControllerInputStrategy probes NControllerManager.Instance before
// checking whether Steamworks exists. On Android that static path can be null
// during early startup, which turns normal fallback controller initialization
// into an unobserved async NullReferenceException.
internal static class ControllerInputPatches
{
    private const BindingFlags AllFlags =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private const string FallbackStrategyFieldName = "_fallbackStrategy";
    private const string ControllerInputMapFieldName = "_controllerInputMap";
    private const string SaveControllerInputMappingMethodName = "SaveControllerInputMapping";
    private static bool _loggedFallbackInit;
    private static bool _loggedInputManagerInit;

    // Current game builds keep separate mouse-and-keyboard and keyboard-only
    // hotkey maps. Older builds used a single keyboard map.
    private static readonly KeyboardMapTarget[] SplitKeyboardMapTargets =
    {
        new("_mKbInputMap", "KeyboardMapping", "DefaultHotkeyInputMap", "SaveMKbInputMapping"),
        new("_fKbInputMap", "KbOnlyMapping", "DefaultKbOnlyInputMap", "SaveFKbInputMapping"),
    };
    private static readonly KeyboardMapTarget[] LegacyKeyboardMapTargets =
    {
        new("_keyboardInputMap", "KeyboardMapping", "DefaultKeyboardInputMap", "SaveKeyboardInputMapping"),
    };

    internal sealed record KeyboardMapTarget(
        string FieldName,
        string SettingsPropertyName,
        string DefaultPropertyName,
        string SaveMethodName
    );

    internal static void Apply(Harmony harmony)
    {
        if (!OperatingSystem.IsAndroid())
            return;

        PatchNInputManagerInit(harmony);
        PatchSteamControllerInputInit(harmony);
    }

    private static void PatchSteamControllerInputInit(Harmony harmony)
    {
        var target = typeof(SteamControllerInputStrategy).GetMethod("Init", AllFlags);
        var prefix = typeof(ControllerInputPatches).GetMethod(
            nameof(SteamControllerInputInitPrefix),
            AllFlags
        );

        if (target == null || prefix == null)
        {
            PatchHelper.Log("Controller input patch skipped: SteamControllerInputStrategy.Init not found");
            return;
        }

        harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        PatchHelper.Log("Patched SteamControllerInputStrategy.Init Android fallback");
    }

    private static void PatchNInputManagerInit(Harmony harmony)
    {
        var target = typeof(NInputManager).GetMethod("Init", AllFlags);
        var prefix = typeof(ControllerInputPatches).GetMethod(
            nameof(NInputManagerInitPrefix),
            AllFlags
        );

        if (target == null || prefix == null)
        {
            PatchHelper.Log("Controller input patch skipped: NInputManager.Init not found");
            return;
        }

        harmony.Patch(target, prefix: new HarmonyMethod(prefix));
        PatchHelper.Log("Patched NInputManager.Init Android fallback");
    }

    private static bool SteamControllerInputInitPrefix(
        SteamControllerInputStrategy __instance,
        ref Task __result
    )
    {
        var fallback = GetFallbackStrategy(__instance);
        if (fallback == null)
        {
            PatchHelper.Log("Controller input patch skipped: fallback strategy field unavailable");
            return true;
        }

        __result = InitAndroidFallback(fallback);
        return false;
    }

    private static IControllerInputStrategy GetFallbackStrategy(
        SteamControllerInputStrategy instance
    )
    {
        var field = typeof(SteamControllerInputStrategy).GetField(FallbackStrategyFieldName, AllFlags);
        return field?.GetValue(instance) as IControllerInputStrategy;
    }

    private static async Task InitAndroidFallback(IControllerInputStrategy fallback)
    {
        await fallback.Init();

        if (_loggedFallbackInit)
            return;

        _loggedFallbackInit = true;
        PatchHelper.Log("Controller input patch initialized Godot fallback strategy on Android");
    }

    private static bool NInputManagerInitPrefix(NInputManager __instance, ref Task __result)
    {
        __result = InitNInputManagerAndroid(__instance);
        return false;
    }

    private static async Task InitNInputManagerAndroid(NInputManager inputManager)
    {
        var controllerManager = inputManager.ControllerManager;
        if (controllerManager == null)
        {
            PatchHelper.Log("Controller input patch: NInputManager.ControllerManager was null");
        }
        else
        {
            await controllerManager.Init();
        }

        var settings = GetSettingsSave();
        ApplyKeyboardMapping(inputManager, settings);
        ApplyControllerMapping(inputManager, controllerManager, settings);

        if (_loggedInputManagerInit)
            return;

        _loggedInputManagerInit = true;
        PatchHelper.Log("Controller input patch initialized NInputManager mappings on Android");
    }

    private static SettingsSave GetSettingsSave()
    {
        var settings = SaveManager.Instance.SettingsSave;
        if (settings != null)
            return settings;

        PatchHelper.Log("Controller input patch: SettingsSave was null; reinitializing settings data");
        SaveManager.Instance.InitSettingsData();
        return SaveManager.Instance.SettingsSave ?? new SettingsSave();
    }

    private static void ApplyKeyboardMapping(NInputManager inputManager, SettingsSave settings)
    {
        var targets = ResolveKeyboardMapTargets(typeof(NInputManager), typeof(SettingsSave));
        if (targets.Count == 0)
        {
            PatchHelper.Log("Controller input patch: no supported keyboard map layout found");
            return;
        }

        foreach (var target in targets)
            ApplyKeyboardMap(inputManager, settings, target);
    }

    // Mirrors NInputManager.Init: saved bindings overlay the defaults, and an
    // empty saved mapping is replaced by the defaults and written back.
    private static void ApplyKeyboardMap(
        NInputManager inputManager,
        SettingsSave settings,
        KeyboardMapTarget target
    )
    {
        var saved = typeof(SettingsSave).GetProperty(target.SettingsPropertyName, AllFlags)
            ?.GetValue(settings) as Dictionary<string, string>;
        var defaults = typeof(NInputManager).GetProperty(target.DefaultPropertyName, AllFlags)
            ?.GetValue(null) as Dictionary<StringName, Key>;
        var map = defaults != null
            ? new Dictionary<StringName, Key>(defaults)
            : new Dictionary<StringName, Key>();

        var hasSavedMapping = saved != null && saved.Count > 0;
        if (hasSavedMapping)
        {
            foreach (var item in saved)
            {
                if (Enum.TryParse<Key>(item.Value, out var key))
                    map[item.Key] = key;
            }
        }

        SetField(inputManager, target.FieldName, map);
        if (!hasSavedMapping)
            Invoke(inputManager, target.SaveMethodName);
    }

    // Returns the first layout whose map field, saved setting, default map and
    // save method all exist on this game build.
    internal static IReadOnlyList<KeyboardMapTarget> ResolveKeyboardMapTargets(
        Type inputManagerType,
        Type settingsType
    )
    {
        foreach (var layout in new[] { SplitKeyboardMapTargets, LegacyKeyboardMapTargets })
        {
            if (Array.TrueForAll(layout, target => IsSupported(target, inputManagerType, settingsType)))
                return layout;
        }

        return Array.Empty<KeyboardMapTarget>();
    }

    private static bool IsSupported(KeyboardMapTarget target, Type inputManagerType, Type settingsType)
        => inputManagerType.GetField(target.FieldName, AllFlags) != null
            && settingsType.GetProperty(target.SettingsPropertyName, AllFlags) != null
            && inputManagerType.GetProperty(target.DefaultPropertyName, AllFlags) != null
            && inputManagerType.GetMethod(target.SaveMethodName, AllFlags, null, Type.EmptyTypes, null) != null;

    private static void ApplyControllerMapping(
        NInputManager inputManager,
        NControllerManager controllerManager,
        SettingsSave settings
    )
    {
        var saveMapping = false;
        var map = new Dictionary<StringName, StringName>();

        if (
            controllerManager != null
            && settings.ControllerMapping != null
            && settings.ControllerMapping.Count > 0
            && settings.ControllerMappingType == controllerManager.ControllerMappingType
        )
        {
            foreach (var item in settings.ControllerMapping)
                map[item.Key] = item.Value;
        }

        if (map.Count == 0)
        {
            map = controllerManager?.GetDefaultControllerInputMap
                ?? new SteamControllerConfig().DefaultControllerInputMap;
            saveMapping = true;
        }

        SetField(inputManager, ControllerInputMapFieldName, map);
        if (saveMapping)
            Invoke(inputManager, SaveControllerInputMappingMethodName);
    }

    private static void SetField(object instance, string name, object value)
    {
        var field = instance.GetType().GetField(name, AllFlags);
        if (field == null)
        {
            PatchHelper.Log($"Controller input patch: field {name} not found");
            return;
        }

        field.SetValue(instance, value);
    }

    private static void Invoke(object instance, string name)
    {
        var method = instance.GetType().GetMethod(name, AllFlags);
        if (method == null)
        {
            PatchHelper.Log($"Controller input patch: method {name} not found");
            return;
        }

        method.Invoke(instance, null);
    }
}
