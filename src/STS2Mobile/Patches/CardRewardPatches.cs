using System;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Fixes a crash when closing the rewards screen caused by a tween race condition.
// Kills the fade tween and stops processing before QueueFree so _Process doesn't
// fire after the node is removed from the tree.
internal static class CardRewardPatches
{
    private const string AfterOverlayClosedMethod = "AfterOverlayClosed";
    private const string FadeTweenField = "_fadeTween";
    private const string TypeName = "MegaCrit.Sts2.Core.Nodes.Screens.NRewardsScreen";

    internal static void Apply(Harmony harmony)
    {
        var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;
        var rewardsScreenType = sts2Asm.GetType(TypeName);
        if (rewardsScreenType == null)
            return;

        PatchHelper.Patch(
            harmony,
            rewardsScreenType,
            AfterOverlayClosedMethod,
            prefix: PatchHelper.Method(
                typeof(CardRewardPatches),
                nameof(RewardsScreenClosedPrefix)
            )
        );
    }

    private static void RewardsScreenClosedPrefix(object __instance)
    {
        try
        {
            var node = (Node)__instance;
            node.SetProcess(false);

            var field = AccessTools.Field(__instance.GetType(), FadeTweenField);
            var tween = field?.GetValue(__instance) as Tween;
            if (tween == null || !tween.IsValid())
                return;

            tween.Kill();
            field.SetValue(__instance, null);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"RewardsScreenClosedPrefix failed: {ex.Message}");
        }
    }
}
