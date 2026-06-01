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
internal static class FontSubstitutionPatches
{
    private const BindingFlags FinalizerBindingFlags =
        BindingFlags.NonPublic | BindingFlags.Static;
    private const string MethodName = "ApplyLocaleFontSubstitution";
    private const string TypeName = "MegaCrit.Sts2.Core.Localization.Fonts.FontControlUtils";

    private const BindingFlags TargetBindingFlags =
        BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Static
            | BindingFlags.Instance;

    private static bool _loggedFirstSwallow;

    internal static void Apply(Harmony harmony)
    {
        var target = FindTarget();
        if (target == null)
            return;

        var finalizer = typeof(FontSubstitutionPatches).GetMethod(
            nameof(ApplyLocaleFontSubstitutionFinalizer),
            FinalizerBindingFlags
        );
        harmony.Patch(target, finalizer: new HarmonyMethod(finalizer));
        PatchHelper.Log("Patched FontControlUtils.ApplyLocaleFontSubstitution finalizer");
    }

    private static MethodInfo FindTarget()
    {
        var sts2Asm = typeof(NGame).Assembly;
        var fontUtilsType = sts2Asm.GetType(TypeName);
        if (fontUtilsType == null)
        {
            PatchHelper.Log("FontSubstitutionPatches: FontControlUtils type not found; skipping");
            return null;
        }

        var target = fontUtilsType.GetMethod(MethodName, TargetBindingFlags);
        if (target == null)
        {
            PatchHelper.Log(
                "FontSubstitutionPatches: ApplyLocaleFontSubstitution method not found; skipping"
            );
        }

        return target;
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
