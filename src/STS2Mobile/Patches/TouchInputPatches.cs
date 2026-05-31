using System;
using System.Reflection;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Cancels card play when the touch is released outside the play zone.
// The desktop game relies on mouse-up position, but on mobile the drag target
// can drift below the play zone threshold during a swipe.
internal static class TouchInputPatches
{
    internal static void Apply(Harmony harmony)
    {
        var mouseCardPlayType = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly.GetType(
            "MegaCrit.Sts2.Core.Nodes.Combat.NMouseCardPlay"
        );
        if (mouseCardPlayType == null)
            return;

        PatchHelper.Patch(
            harmony,
            mouseCardPlayType,
            "_Input",
            postfix: PatchHelper.Method(
                typeof(TouchInputPatches),
                nameof(MouseCardPlayInputPostfix)
            )
        );
    }

    // On left mouse button release, check if the card is still in the play zone.
    // If not, cancel the card play to prevent accidental plays from imprecise touches.
    private static void MouseCardPlayInputPostfix(object __instance, object inputEvent)
    {
        try
        {
            CancelIfReleasedOutsidePlayZone(__instance, inputEvent);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"MouseCardPlayInputPostfix: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void CancelIfReleasedOutsidePlayZone(object cardPlay, object inputEvent)
    {
        if (
            inputEvent is not InputEventMouseButton mouseButton
            || mouseButton.ButtonIndex != MouseButton.Left
            || !mouseButton.IsReleased()
        )
            return;

        var instanceType = cardPlay.GetType();
        var isInPlayZone = instanceType.GetMethod(
            "IsCardInPlayZone",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        if (isInPlayZone == null || (bool)isInPlayZone.Invoke(cardPlay, null))
            return;

        var cancel = instanceType.GetMethod(
            "CancelPlayCard",
            BindingFlags.Public | BindingFlags.Instance
        );
        cancel?.Invoke(cardPlay, null);
        PatchHelper.Log("Card play cancelled: touch released below play zone");
    }
}
