using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Settings;

namespace STS2Mobile.Patches;

// Repositions main menu elements for mobile viewports. Scales the background
// image to fill taller screens and adjusts button/logo placement when UI scale
// is above 100%.
internal static class MobileLayoutPatches
{
    private const float BackgroundHeight = 1200f;

    internal static void Apply(Harmony harmony)
    {
        var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;
        var nMainMenuType = sts2Asm.GetType("MegaCrit.Sts2.Core.Nodes.Screens.MainMenu.NMainMenu");
        if (nMainMenuType == null)
            return;

        PatchHelper.Patch(
            harmony,
            nMainMenuType,
            "_Ready",
            postfix: PatchHelper.Method(
                typeof(MobileLayoutPatches),
                nameof(MainMenuReadyPostfix)
            )
        );
    }

    private static void MainMenuReadyPostfix(object __instance)
    {
        try
        {
            var menu = (Node)__instance;
            ApplyMainMenuMobileLayout(menu);
            UiScalePatches.UiScaleChanged += OnScaleChanged;

            void OnScaleChanged()
            {
                if (!GodotObject.IsInstanceValid((GodotObject)menu) || !menu.IsInsideTree())
                {
                    UiScalePatches.UiScaleChanged -= OnScaleChanged;
                    return;
                }

                ApplyMainMenuMobileLayout(menu);
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"MainMenuReadyPostfix failed: {ex.Message}");
        }
    }

    private static void ApplyMainMenuMobileLayout(Node menu)
    {
        var window = menu.GetTree().Root;
        var vpSize = window.ContentScaleSize;

        // Scale background to fill the viewport on taller screens.
        if (SaveManager.Instance.SettingsSave.AspectRatioSetting == AspectRatioSetting.Auto)
            ScaleBackground(menu, window.GetVisibleRect().Size);

        // Reposition buttons and logo when UI scale is above 100%.
        UiScalePatches.EnsureUiScaleLoaded();
        if (UiScalePatches.UiScalePercent <= 100)
            return;

        float viewportWidth = vpSize.X;

        ApplyButtonLayout(menu);
        ApplyLogoLayout(menu, viewportWidth);

        PatchHelper.Log("Main menu: repositioned buttons left, logo right");
    }

    private static void ApplyButtonLayout(Node menu)
    {
        var buttons = menu.GetNodeOrNull<Control>("%MainMenuTextButtons");
        if (buttons == null)
            return;

        buttons.AnchorLeft = 0f;
        buttons.AnchorRight = 0.5f;
        buttons.AnchorTop = 0f;
        buttons.AnchorBottom = 1f;
        buttons.OffsetLeft = 0f;
        buttons.OffsetRight = 0f;
        buttons.OffsetTop = 0f;
        buttons.OffsetBottom = 0f;
        buttons.GrowHorizontal = Control.GrowDirection.Both;
        buttons.GrowVertical = Control.GrowDirection.Both;
        buttons.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        buttons.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
    }

    private static void ApplyLogoLayout(Node menu, float viewportWidth)
    {
        var bg = menu.GetNodeOrNull<Node>("%MainMenuBg");
        var logo = bg?.GetNodeOrNull<Node2D>("%Logo");
        if (logo == null)
            return;

        var pos = logo.Position;
        logo.Position = new Vector2(viewportWidth * 0.25f + pos.X, pos.Y);
    }

    private static void ScaleBackground(Node menu, Vector2 viewportSize)
    {
        try
        {
            var bgContainer = FindBackgroundContainer(menu);
            if (bgContainer == null)
                return;

            float viewportHeight = viewportSize.Y;
            if (viewportHeight <= BackgroundHeight)
            {
                bgContainer.Scale = Vector2.One;
                return;
            }

            float scale = viewportHeight / BackgroundHeight;
            bgContainer.Scale = new Vector2(scale, scale);
            PatchHelper.Log($"Main menu bg: scaled by {scale:F3} for viewport height {viewportHeight}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"ScaleMainMenuBg failed: {ex.Message}");
        }
    }

    private static Control FindBackgroundContainer(Node menu)
    {
        var bg = menu.GetNodeOrNull<Control>("%MainMenuBg");
        return bg?.GetNodeOrNull<Control>("BgContainer");
    }
}
