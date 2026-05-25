using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace STS2Mobile.Patches;

// Post-game-update workaround: FontControlUtils.ApplyLocaleFontSubstitution
// can throw on Android while MegaLabel/NMegaTextEdit nodes initialize. If that
// exception escapes _Ready(), the UI fails to finish building and the user sees
// a black screen. The finalizer suppresses that failure and lets labels fall
// back to the default theme font.
public static class FontSubstitutionPatches
{
    private static bool _loggedFirstSwallow;

    public static void Apply(Harmony harmony)
    {
        var sts2Asm = typeof(NGame).Assembly;
        var fontUtilsType = sts2Asm.GetType(
            "MegaCrit.Sts2.Core.Localization.Fonts.FontControlUtils"
        );
        if (fontUtilsType == null)
        {
            PatchHelper.Log("FontSubstitutionPatches: FontControlUtils type not found; skipping");
            return;
        }

        var target = fontUtilsType.GetMethod(
            "ApplyLocaleFontSubstitution",
            BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Static
                | BindingFlags.Instance
        );
        if (target == null)
        {
            PatchHelper.Log(
                "FontSubstitutionPatches: ApplyLocaleFontSubstitution method not found; skipping"
            );
            return;
        }

        var finalizer = typeof(FontSubstitutionPatches).GetMethod(
            nameof(ApplyLocaleFontSubstitutionFinalizer),
            BindingFlags.NonPublic | BindingFlags.Static
        );
        harmony.Patch(target, finalizer: new HarmonyMethod(finalizer));
        PatchHelper.Log("Patched FontControlUtils.ApplyLocaleFontSubstitution finalizer");
    }

    private static Exception ApplyLocaleFontSubstitutionFinalizer(Exception __exception)
    {
        if (__exception == null)
            return null;

        if (!_loggedFirstSwallow)
        {
            _loggedFirstSwallow = true;
            PatchHelper.Log(
                $"FontSubstitutionPatches: suppressed {__exception.GetType().Name} "
                    + $"from ApplyLocaleFontSubstitution ({__exception.Message}); further occurrences silenced"
            );
        }

        return null;
    }
}

