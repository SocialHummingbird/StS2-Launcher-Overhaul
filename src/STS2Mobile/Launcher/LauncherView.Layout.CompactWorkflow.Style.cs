using System;
using Godot;
using STS2Mobile.Launcher.Components;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private static Button BuildCompactWorkflowStepButton(int index, float scale, int height)
    {
        var button = new Button
        {
            Text = "",
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = $"Go to {CompactWorkflowStepTooltips[index]}",
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(height, scale)
            ),
        };
        ApplyWorkflowStepButtonStyle(button, scale);
        return button;
    }

    private static void ApplyWorkflowStepButtonStyle(Button button, float scale)
    {
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateNormal,
            BuildWorkflowStepStyle(
                scale,
                new Color(0.025f, 0.045f, 0.06f, 0.82f),
                new Color(0.05f, 0.34f, 0.42f, 0.45f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateHover,
            BuildWorkflowStepStyle(
                scale,
                new Color(0.035f, 0.075f, 0.095f, 0.9f),
                new Color(0.06f, 0.54f, 0.62f, 0.58f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StatePressed,
            BuildWorkflowStepStyle(
                scale,
                new Color(0.02f, 0.035f, 0.05f, 0.95f),
                new Color(0.95f, 0.42f, 0.08f, 0.72f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateDisabled,
            BuildWorkflowStepStyle(
                scale,
                new Color(0.025f, 0.035f, 0.045f, 0.62f),
                new Color(0.05f, 0.16f, 0.2f, 0.36f)
            )
        );
    }

    private static StyleBoxFlat BuildWorkflowStepStyle(float scale)
        => BuildWorkflowStepStyle(
            scale,
            new Color(0.025f, 0.045f, 0.06f, 0.82f),
            new Color(0.05f, 0.34f, 0.42f, 0.45f)
        );

    private static StyleBoxFlat BuildWorkflowStepStyle(float scale, Color body, Color border)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            body,
            LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepRadius, scale)
        );
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
        return style;
    }
}
