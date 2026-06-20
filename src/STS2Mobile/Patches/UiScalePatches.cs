using System;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Replaces the desktop resolution dropdown with a UI scale selector for mobile.
// Persists the scale percentage to user://ui_scale.cfg and applies it by adjusting
// the window's ContentScaleSize. Also intercepts window change handlers to maintain
// the correct scale when the viewport resizes.
internal static partial class UiScalePatches
{
    private const int BaseHeight = 1080;
    private const int BaseWidth = 1680;
    private const string ConfigPath = "user://ui_scale.cfg";
    private const int MaxPercent = 200;
    private const int MinPercent = 100;

    internal static int UiScalePercent { get; private set; } = 100;
    internal static event Action UiScaleChanged;
    private static bool _uiScaleLoaded = false;

    internal static void Apply(Harmony harmony)
    {
        var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;

        PatchHelper.Log(
            "UI scale dropdown replacement disabled: current Android runtime cannot safely Harmony-wrap private-field-heavy resolution dropdown methods."
        );
        PatchWindowChangeHandlers(harmony, sts2Asm);
    }
}
