using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2Mobile.Patches;

internal static partial class PlatformPatches
{
    private static void ApplyNullPlatformLanguagePatch(Harmony harmony)
    {
        try
        {
            var method = FindGetThreeLetterLanguageCode();
            if (method == null)
                return;

            harmony.Patch(
                method,
                prefix: new HarmonyMethod(Prefix(nameof(GetThreeLetterLanguageCodePrefix)))
            );
            PatchHelper.Log(
                "Patched NullPlatformUtilStrategy.GetThreeLetterLanguageCode (locale fix)"
            );
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"Locale fix failed: {ex.Message}");
        }
    }

    private static MethodInfo FindGetThreeLetterLanguageCode()
    {
        var sts2Asm = typeof(NGame).Assembly;
        var nullStrategyType = sts2Asm.GetType(
            "MegaCrit.Sts2.Core.Platform.Null.NullPlatformUtilStrategy"
        );
        if (nullStrategyType == null)
        {
            PatchHelper.Log("Locale fix: NullPlatformUtilStrategy not found, skipping");
            return null;
        }

        var method = nullStrategyType.GetMethod(
            "GetThreeLetterLanguageCode",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (method == null)
            PatchHelper.Log("Locale fix: GetThreeLetterLanguageCode not found, skipping");

        return method;
    }
}
