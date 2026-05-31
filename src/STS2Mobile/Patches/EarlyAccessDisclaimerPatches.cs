using System;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Fixes the early access disclaimer layout on non-16:9 screens. The original
// VBoxContainer uses fixed pixel offsets designed for 1680x1080 that misalign
// on wider or narrower viewports. Switches to proportional anchors instead.
internal static class EarlyAccessDisclaimerPatches
{
    private const float DesignedBannerWidth = 1680f;
    private const float DesignedSideInset = 596f;
    private const float TextHalfWidth = 265f;
    private const string ImageNode = "Image";
    private const string ReadyMethod = "_Ready";
    private const string TypeName =
        "MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NEarlyAccessDisclaimer";
    private const string VBoxContainerNode = "VBoxContainer";

    internal static void Apply(Harmony harmony)
    {
        var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;
        var disclaimerType = sts2Asm.GetType(TypeName);
        if (disclaimerType == null)
            return;

        PatchHelper.Patch(
            harmony,
            disclaimerType,
            ReadyMethod,
            postfix: PatchHelper.Method(
                typeof(EarlyAccessDisclaimerPatches),
                nameof(ReadyPostfix)
            )
        );
    }

    private static void ReadyPostfix(object __instance)
    {
        try
        {
            var disclaimer = (Control)__instance;
            ApplyLayout(disclaimer);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[EADisclaimer] ReadyPostfix failed: {ex.Message}");
        }
    }

    private static void ApplyLayout(Control disclaimer)
    {
        var image = disclaimer.GetNode<Control>(ImageNode);
        var vbox = image.GetNode<Control>(VBoxContainerNode);

        // Convert fixed pixel offsets to proportional anchors so the text
        // scales with the banner image on any viewport width.
        float halfWidth = TextHalfWidth / (DesignedBannerWidth - DesignedSideInset * 2f);
        vbox.AnchorLeft = 0.5f - halfWidth;
        vbox.AnchorRight = 0.5f + halfWidth;
        vbox.OffsetLeft = 0f;
        vbox.OffsetRight = 0f;

        PatchHelper.Log(
            $"[EADisclaimer] Fixed VBoxContainer anchors: L={vbox.AnchorLeft:F3} R={vbox.AnchorRight:F3}"
        );
    }
}
