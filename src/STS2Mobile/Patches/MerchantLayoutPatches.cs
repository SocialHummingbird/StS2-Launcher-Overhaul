using System;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Adjusts the merchant shop open animation for shorter viewports. When UI scale
// reduces the effective viewport height below 1080px, the inventory panel's
// target position is shifted up so it remains fully visible.
internal static class MerchantLayoutPatches
{
    private const double BackstopDuration = 1.0;
    private const double SlotsDuration = 0.7;
    private const float BackstopAlpha = 0.8f;
    private const float BaseOpenY = 80f;
    private const float BaseViewportHeight = 1080f;
    private const float LostHeightOffsetMultiplier = 0.5f;
    private const string BackstopAlphaProperty = "modulate:a";
    private const string BackstopField = "_backstop";
    private const string DoOpenAnimationMethod = "DoOpenAnimation";
    private const string InventoryTweenField = "_inventoryTween";
    private const string InventoryTypeName =
        "MegaCrit.Sts2.Core.Nodes.Screens.Shops.NMerchantInventory";
    private const string SlotsContainerField = "_slotsContainer";
    private const string SlotsYProperty = "position:y";

    internal static void Apply(Harmony harmony)
    {
        var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;
        var merchantInvType = sts2Asm.GetType(InventoryTypeName);
        if (merchantInvType == null)
            return;

        PatchHelper.Patch(
            harmony,
            merchantInvType,
            DoOpenAnimationMethod,
            prefix: PatchHelper.Method(
                typeof(MerchantLayoutPatches),
                nameof(MerchantOpenPrefix)
            )
        );
    }

    private static bool MerchantOpenPrefix(object __instance, ref Task __result)
    {
        try
        {
            if (!TryStartScaledOpenAnimation(__instance, out var result))
                return true;

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"MerchantOpenPrefix failed: {ex.Message}");
            return true;
        }
    }

    private static bool TryStartScaledOpenAnimation(object instance, out Task result)
    {
        result = null;
        UiScalePatches.EnsureUiScaleLoaded();

        var node = (Node)instance;
        var window = node.GetTree().Root;
        float scaledHeight = window.ContentScaleSize.Y;
        if (scaledHeight >= BaseViewportHeight)
            return false;

        var instType = instance.GetType();
        var slotsContainer = (Control)
            AccessTools.Field(instType, SlotsContainerField).GetValue(instance);
        var backstop = (Node)AccessTools
            .Field(instType, BackstopField)
            .GetValue(instance);

        var existingTween =
            AccessTools.Field(instType, InventoryTweenField)?.GetValue(instance)
                as Tween;
        float scaledOpenPos =
            BaseOpenY - (BaseViewportHeight - scaledHeight) * LostHeightOffsetMultiplier;
        var tween = StartOpenTween(node, backstop, slotsContainer, existingTween, scaledOpenPos);

        AccessTools.Field(instType, InventoryTweenField)?.SetValue(instance, tween);

        PatchHelper.Log($"Merchant open: y={scaledOpenPos} (viewport height: {scaledHeight})");

        result = Task.CompletedTask;
        return true;
    }

    private static Tween StartOpenTween(
        Node node,
        Node backstop,
        Control slotsContainer,
        Tween existingTween,
        float openY)
    {
        existingTween?.Kill();

        var tween = node.CreateTween().SetParallel();
        tween
            .TweenProperty(backstop, BackstopAlphaProperty, BackstopAlpha, BackstopDuration)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine)
            .FromCurrent();
        tween
            .TweenProperty(slotsContainer, SlotsYProperty, openY, SlotsDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Quint)
            .FromCurrent();

        return tween;
    }
}
