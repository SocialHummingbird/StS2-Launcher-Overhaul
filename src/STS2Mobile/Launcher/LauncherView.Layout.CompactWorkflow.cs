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
    private static LauncherViewCompactWorkflowStrip BuildCompactWorkflowStrip(float scale, bool compact, bool denseNarrowWorkflow)
    {
        if (!compact)
            return new LauncherViewCompactWorkflowStrip(new Control { Visible = false }, Array.Empty<StyledLabel>(), Array.Empty<StyledLabel>(), Array.Empty<StyledLabel>(), Array.Empty<ColorRect>(), Array.Empty<Button>());
        var grid = new GridContainer
        {
            Columns = CompactWorkflowStepNames.Length,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        grid.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepCellGap, scale));
        var stepHeight = denseNarrowWorkflow ? CompactWorkflowStepDenseHeight : CompactWorkflowStepHeight;
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

    private static readonly CompactButtonDetailLabelSpec CompactCurrentTaskButtonLabels = CompactButtonDetailLabelSpec.Default(CompactCurrentTaskButtonBodyName, CompactCurrentTaskButtonTitleName, CompactCurrentTaskButtonDetailName);
    private static Button BuildCompactCurrentTaskButton(float scale, bool compact)
    {
        if (!compact)
            return new Button
            {
                Visible = false
            };
        var button = new StyledButton("", scale, fontSize: LauncherSectionMetrics.CompactDetailButtonFontSize, height: LauncherSectionMetrics.CompactDetailButtonHeight);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        LauncherButtonStyles.ApplySupportAction(button, scale);
        SetCompactCurrentTaskButtonText(button, scale, "Start here", "Setup guide");
        return button;
    }

    private static void SetCompactCurrentTaskButtonText(Button button, float scale, string title, string detail) => CompactButtonDetailLabels.Apply(button, $"{title}\n{detail}", scale, enabled: true, CompactCurrentTaskButtonLabels);
    private const int CompactStickyTaskHeaderInlineGap = 6;
    private const int CompactStickyTaskHeaderStackGap = 3;
    private const int CompactStickyTaskButtonMinWidth = 176;
    private const int CompactInlineCurrentTaskHeight = LauncherSectionMetrics.CompactDetailButtonHeight;
    private const int CompactStackedCurrentTaskHeight = CompactWorkflowStepDenseHeight;
    private const int CompactStickyTaskHeaderStackWidth = 560;
    private const int CompactStickyTaskToolbarRadius = 7;
    private const int CompactStickyTaskToolbarHorizontalMargin = 5;
    private const int CompactStickyTaskToolbarVerticalMargin = 4;
    private const string CompactStickyTaskHeaderGridName = "CompactStickyTaskHeaderGrid";
    private const string CompactCurrentTaskButtonBodyName = "CompactCurrentTaskButtonBody";
    private const string CompactCurrentTaskButtonTitleName = "CompactCurrentTaskButtonTitle";
    private const string CompactCurrentTaskButtonDetailName = "CompactCurrentTaskButtonDetail";
    private static (Control Toolbar, GridContainer Header) BuildCompactStickyTaskHeader(LauncherLayoutProfile profile, Button compactCurrentTaskButton, Control workflowStrip)
    {
        var scale = profile.Scale;
        var header = new GridContainer
        {
            Name = CompactStickyTaskHeaderGridName,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        header.AddChild(compactCurrentTaskButton);
        header.AddChild(workflowStrip);
        ApplyCompactStickyTaskHeaderLayout(header, compactCurrentTaskButton, workflowStrip, profile);
        return (WrapCompactStickyTaskHeader(scale, header), header);
    }

    private static void ApplyCompactStickyTaskHeaderLayout(GridContainer header, Button compactCurrentTaskButton, Control workflowStrip, LauncherLayoutProfile profile)
    {
        var scale = profile.Scale;
        var stacked = ShouldStackCompactStickyTaskHeader(profile);
        header.Columns = stacked ? 1 : 2;
        header.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(stacked ? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap, scale));
        if (stacked)
        {
            compactCurrentTaskButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            compactCurrentTaskButton.CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(CompactStackedCurrentTaskHeight, scale));
            workflowStrip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            workflowStrip.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
            return;
        }

        compactCurrentTaskButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        compactCurrentTaskButton.CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskButtonMinWidth, scale), LauncherViewLayoutMetrics.ScaleInt(CompactInlineCurrentTaskHeight, scale));
        workflowStrip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        workflowStrip.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    }

    private static bool ShouldStackCompactStickyTaskHeader(LauncherLayoutProfile profile) => profile.ContentMaxWidth < LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskHeaderStackWidth, profile.Scale);
    private static Control WrapCompactStickyTaskHeader(float scale, Control header)
    {
        var toolbar = new PanelContainer();
        toolbar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolbar.AddThemeStyleboxOverride(LauncherComponentTheme.Panel, BuildCompactStickyTaskHeaderStyle(scale));
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolbar.AddChild(header);
        return toolbar;
    }

    private static StyleBoxFlat BuildCompactStickyTaskHeaderStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(new Color(0.018f, 0.035f, 0.045f, 0.9f), LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarRadius, scale));
        style.BorderColor = new Color(0.04f, 0.42f, 0.5f, 0.45f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarVerticalMargin, scale);
        return style;
    }

    private static VBoxContainer BuildCompactWorkflowStepBody(float scale)
    {
        var body = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
        body.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepSeparation, scale));
        return body;
    }

    private static HBoxContainer BuildCompactWorkflowLabelRow(float scale)
    {
        var labelRow = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        labelRow.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepNumberGap, scale));
        return labelRow;
    }

    private static ColorRect BuildCompactWorkflowAccent(float scale)
    {
        return new ColorRect
        {
            Color = LauncherComponentTheme.ButtonNormal,
            CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepAccentHeight, scale)),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }

    private static LauncherViewCompactWorkflowStepCell BuildCompactWorkflowStepCell(int index, float scale, int stepHeight)
    {
        var button = BuildCompactWorkflowStepButton(index, scale, stepHeight);
        var body = BuildCompactWorkflowStepBody(scale);
        button.AddChild(body);
        var labelRow = BuildCompactWorkflowLabelRow(scale);
        body.AddChild(labelRow);
        var numberLabel = BuildCompactWorkflowNumberLabel(index, scale);
        labelRow.AddChild(numberLabel);
        var label = BuildCompactWorkflowLabel(index, scale);
        labelRow.AddChild(label);
        var detail = BuildCompactWorkflowDetailLabel(index, scale);
        body.AddChild(detail);
        var accent = BuildCompactWorkflowAccent(scale);
        body.AddChild(accent);
        return new LauncherViewCompactWorkflowStepCell(button, numberLabel, label, detail, accent);
    }

    private static StyledLabel BuildCompactWorkflowNumberLabel(int index, float scale)
    {
        var numberLabel = new StyledLabel(CompactWorkflowStepNumbers[index], scale, fontSize: CompactWorkflowStepNumberFontSize, align: HorizontalAlignment.Center);
        numberLabel.CustomMinimumSize = new Vector2(LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepNumberMinWidth, scale), 0);
        numberLabel.VerticalAlignment = VerticalAlignment.Center;
        numberLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        numberLabel.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextMuted);
        return numberLabel;
    }

    private static StyledLabel BuildCompactWorkflowLabel(int index, float scale)
    {
        var label = new StyledLabel(CompactWorkflowStepNames[index], scale, fontSize: CompactWorkflowStepLabelFontSize, align: HorizontalAlignment.Left);
        label.VerticalAlignment = VerticalAlignment.Center;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.ClipText = true;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextMuted);
        return label;
    }

    private static StyledLabel BuildCompactWorkflowDetailLabel(int index, float scale)
    {
        var detail = new StyledLabel(CompactWorkflowStepDetails[index], scale, fontSize: CompactWorkflowStepDetailFontSize, align: HorizontalAlignment.Center);
        detail.VerticalAlignment = VerticalAlignment.Center;
        detail.MouseFilter = Control.MouseFilterEnum.Ignore;
        detail.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        detail.ClipText = true;
        detail.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        detail.AddThemeColorOverride(LauncherViewLayoutMetrics.ThemeFontColor, LauncherComponentTheme.TextMuted);
        return detail;
    }

    private static Button BuildCompactWorkflowStepButton(int index, float scale, int height)
    {
        var button = new Button
        {
            Text = "",
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = $"Go to {CompactWorkflowStepTooltips[index]}",
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            CustomMinimumSize = new Vector2(0, LauncherViewLayoutMetrics.ScaleInt(height, scale)),
        };
        ApplyWorkflowStepButtonStyle(button, scale);
        return button;
    }

    private static void ApplyWorkflowStepButtonStyle(Button button, float scale)
    {
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateNormal, BuildWorkflowStepStyle(scale, new Color(0.025f, 0.045f, 0.06f, 0.82f), new Color(0.05f, 0.34f, 0.42f, 0.45f)));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateHover, BuildWorkflowStepStyle(scale, new Color(0.035f, 0.075f, 0.095f, 0.9f), new Color(0.06f, 0.54f, 0.62f, 0.58f)));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StatePressed, BuildWorkflowStepStyle(scale, new Color(0.02f, 0.035f, 0.05f, 0.95f), new Color(0.95f, 0.42f, 0.08f, 0.72f)));
        button.AddThemeStyleboxOverride(LauncherComponentTheme.StateDisabled, BuildWorkflowStepStyle(scale, new Color(0.025f, 0.035f, 0.045f, 0.62f), new Color(0.05f, 0.16f, 0.2f, 0.36f)));
    }

    private static StyleBoxFlat BuildWorkflowStepStyle(float scale) => BuildWorkflowStepStyle(scale, new Color(0.025f, 0.045f, 0.06f, 0.82f), new Color(0.05f, 0.34f, 0.42f, 0.45f));
    private static StyleBoxFlat BuildWorkflowStepStyle(float scale, Color body, Color border)
    {
        var style = LauncherStyleBoxes.MakeFilled(body, LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepRadius, scale));
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
        return style;
    }
}

internal readonly struct LauncherViewCompactWorkflowStrip
{
    internal LauncherViewCompactWorkflowStrip(Control strip, StyledLabel[] stepNumberLabels, StyledLabel[] stepLabels, StyledLabel[] stepDetailLabels, ColorRect[] stepAccents, Button[] stepButtons)
    {
        Strip = strip;
        StepNumberLabels = stepNumberLabels;
        StepLabels = stepLabels;
        StepDetailLabels = stepDetailLabels;
        StepAccents = stepAccents;
        StepButtons = stepButtons;
    }

    internal Control Strip { get; }
    internal StyledLabel[] StepNumberLabels { get; }
    internal StyledLabel[] StepLabels { get; }
    internal StyledLabel[] StepDetailLabels { get; }
    internal ColorRect[] StepAccents { get; }
    internal Button[] StepButtons { get; }
}

internal readonly struct LauncherViewCompactWorkflowStepCell
{
    internal LauncherViewCompactWorkflowStepCell(Button button, StyledLabel numberLabel, StyledLabel label, StyledLabel detailLabel, ColorRect accent)
    {
        Button = button;
        NumberLabel = numberLabel;
        Label = label;
        DetailLabel = detailLabel;
        Accent = accent;
    }

    internal Button Button { get; }
    internal StyledLabel NumberLabel { get; }
    internal StyledLabel Label { get; }
    internal StyledLabel DetailLabel { get; }
    internal ColorRect Accent { get; }
}
