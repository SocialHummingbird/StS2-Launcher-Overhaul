using System;
using Godot;
using STS2Mobile.Launcher.Components;
using STS2Mobile.Launcher.Sections;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    private const int CompactWorkflowStepHeight = LauncherSectionMetrics.CompactDetailButtonHeight;
    private const int CompactWorkflowStepDenseHeight = LauncherSectionMetrics.CompactDetailButtonHeight;
    private const int CompactWorkflowStepLabelFontSize = 13;
    private const int CompactWorkflowStepDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactWorkflowStepNumberFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactWorkflowStepNumberMinWidth = 20;
    private const int CompactWorkflowStepAccentHeight = 2;
    private const int CompactWorkflowStepSeparation = 0;
    private const int CompactWorkflowStepCellGap = 3;
    private const int CompactWorkflowStepNumberGap = 3;
    private const int CompactWorkflowStepRadius = 6;
    private const int CompactWorkflowStepHorizontalMargin = 5;
    private const int CompactWorkflowStepVerticalMargin = 4;

    private static LauncherViewCompactWorkflowStrip BuildCompactWorkflowStrip(
        float scale,
        bool compact,
        bool denseNarrowWorkflow
    )
    {
        if (!compact)
            return new LauncherViewCompactWorkflowStrip(
                new Control { Visible = false },
                Array.Empty<StyledLabel>(),
                Array.Empty<StyledLabel>(),
                Array.Empty<StyledLabel>(),
                Array.Empty<ColorRect>(),
                Array.Empty<Button>()
            );

        var grid = new GridContainer
        {
            Columns = CompactWorkflowStepNames.Length,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepCellGap, scale)
        );

        var stepHeight = denseNarrowWorkflow
            ? CompactWorkflowStepDenseHeight
            : CompactWorkflowStepHeight;
        var numberLabels = new StyledLabel[CompactWorkflowStepNames.Length];
        var labels = new StyledLabel[CompactWorkflowStepNames.Length];
        var detailLabels = new StyledLabel[CompactWorkflowStepNames.Length];
        var accents = new ColorRect[CompactWorkflowStepNames.Length];
        var buttons = new Button[CompactWorkflowStepNames.Length];
        for (var i = 0; i < CompactWorkflowStepNames.Length; i++)
        {
            var cell = BuildCompactWorkflowStepCell(i, scale, stepHeight);
            numberLabels[i] = cell.NumberLabel;
            labels[i] = cell.Label;
            detailLabels[i] = cell.DetailLabel;
            accents[i] = cell.Accent;
            buttons[i] = cell.Button;
            grid.AddChild(cell.Button);
        }

        return new LauncherViewCompactWorkflowStrip(grid, numberLabels, labels, detailLabels, accents, buttons);
    }
}
