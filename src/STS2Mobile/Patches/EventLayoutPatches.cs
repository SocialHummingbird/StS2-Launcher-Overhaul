using System;
using Godot;
using HarmonyLib;

namespace STS2Mobile.Patches;

// Adjusts event screen layout and button sizes for scaled viewports. Shifts the
// event panel upward and clamps button widths to the available viewport width
// when UI scale is above 100%.
internal static class EventLayoutPatches
{
    private const string AddOptionsMethod = "AddOptions";
    private const float ButtonHorizontalMargin = 40f;
    private const float ButtonOriginalWidth = 800f;
    private const float CenterAnchor = 0.5f;
    private const int DefaultScalePercent = 100;
    private const float OriginalOffsetLeft = -38f;
    private const float OriginalOffsetRight = 762f;
    private const string OptionsContainerPath = "VBoxContainer/OptionsContainer";
    private const string ReadyMethod = "_Ready";
    private const string TypeName = "MegaCrit.Sts2.Core.Nodes.Events.NEventLayout";
    private const float VerticalShiftMultiplier = 0.5f;
    private const string VBoxContainerPath = "VBoxContainer";

    internal static void Apply(Harmony harmony)
    {
        var sts2Asm = typeof(MegaCrit.Sts2.Core.Nodes.NGame).Assembly;
        var eventLayoutType = sts2Asm.GetType(TypeName);
        if (eventLayoutType == null)
            return;

        PatchHelper.Patch(
            harmony,
            eventLayoutType,
            ReadyMethod,
            postfix: PatchHelper.Method(
                typeof(EventLayoutPatches),
                nameof(ReadyPostfix)
            )
        );

        PatchHelper.Patch(
            harmony,
            eventLayoutType,
            AddOptionsMethod,
            postfix: PatchHelper.Method(
                typeof(EventLayoutPatches),
                nameof(AddOptionsPostfix)
            )
        );
    }

    private static void ReadyPostfix(object __instance)
    {
        try
        {
            var layout = (Control)__instance;
            ApplyLayout(layout);
            UiScalePatches.UiScaleChanged += OnScaleChanged;

            void OnScaleChanged()
            {
                if (!GodotObject.IsInstanceValid(layout) || !layout.IsInsideTree())
                {
                    UiScalePatches.UiScaleChanged -= OnScaleChanged;
                    return;
                }

                ApplyLayout(layout);
                ApplyButtonSizes(layout);
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"EventLayoutReadyPostfix failed: {ex.Message}");
        }
    }

    private static void AddOptionsPostfix(object __instance)
    {
        try
        {
            var layout = (Control)__instance;
            ApplyButtonSizes(layout);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"EventAddOptionsPostfix failed: {ex.Message}");
        }
    }

    internal static void ApplyLayout(Control layout)
    {
        UiScalePatches.EnsureUiScaleLoaded();

        var window = layout.GetTree().Root;
        float vpWidth = window.ContentScaleSize.X;

        var vbox = layout.GetNodeOrNull<Control>(VBoxContainerPath);
        if (vbox == null)
            return;

        if (IsDefaultScale(UiScalePatches.UiScalePercent))
        {
            // Reset to original scene values
            layout.Position = new Vector2(layout.Position.X, 0f);
            vbox.OffsetLeft = OriginalOffsetLeft;
            vbox.OffsetRight = OriginalOffsetRight;
            return;
        }

        float shiftUp = VerticalShiftForScale(layout.Size.Y, UiScalePatches.UiScalePercent);
        layout.Position = new Vector2(layout.Position.X, -shiftUp);

        float buttonWidth = ButtonWidthForViewport(vpWidth, UiScalePatches.UiScalePercent);
        float half = buttonWidth / 2f;
        vbox.AnchorLeft = CenterAnchor;
        vbox.AnchorRight = CenterAnchor;
        vbox.OffsetLeft = -half;
        vbox.OffsetRight = half;
    }

    internal static void ApplyButtonSizes(Control layout)
    {
        UiScalePatches.EnsureUiScaleLoaded();

        var window = layout.GetTree().Root;
        float targetWidth = ButtonWidthForViewport(
            window.ContentScaleSize.X,
            UiScalePatches.UiScalePercent
        );

        var optionsContainer = layout.GetNodeOrNull(OptionsContainerPath);
        if (optionsContainer == null)
            return;

        foreach (var child in optionsContainer.GetChildren())
        {
            if (child is Control btn)
                btn.CustomMinimumSize = new Vector2(targetWidth, btn.CustomMinimumSize.Y);
        }
    }

    private static float ButtonWidthForViewport(float viewportWidth, int scalePercent)
    {
        if (IsDefaultScale(scalePercent))
            return ButtonOriginalWidth;

        float maxWidth = viewportWidth - ButtonHorizontalMargin * 2f;
        return Math.Min(ButtonOriginalWidth, maxWidth);
    }

    private static bool IsDefaultScale(int scalePercent) => scalePercent <= DefaultScalePercent;

    private static float VerticalShiftForScale(float layoutHeight, int scalePercent)
    {
        float scale = scalePercent / (float)DefaultScalePercent;
        return layoutHeight * (1f - 1f / scale) * VerticalShiftMultiplier;
    }
}
