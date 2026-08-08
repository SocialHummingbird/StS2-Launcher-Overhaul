using System;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2Mobile.Patches;

internal static partial class SettingsPatches
{
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
