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
    private const int CompactCurrentTaskButtonTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactCurrentTaskButtonDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactCurrentTaskButtonHorizontalMargin = 6;
    private const int CompactCurrentTaskButtonVerticalMargin = 4;
    private const int CompactStatusBodySeparation = 5;
    private const int CompactStatusAccentHeight = 3;
    private const int CompactStatusHeadlineSeparation = 3;
    private const int CompactStatusHeadlineInlineSeparation = 6;
    private const int CompactStatusPhaseInlineWidth = 112;
    private const int CompactStatusPhaseHorizontalMargin = 7;
    private const int CompactStatusPhaseVerticalMargin = 3;
    private const int CompactStatusActionMinHeight = 24;
    private const int CompactStatusDetailHeight = 44;
    private const int CompactStatusDetailCueWidth = 62;
    private const int CompactStatusDetailCueFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactStatusDetailHorizontalMargin = 8;
    private const int CompactStatusDetailVerticalMargin = 5;
    private const int CompactStatusDetailRowGap = 6;
    private const int CompactStatusDetailRadius = 7;
    private const string CompactSafeFlowToggleBodyName = "CompactSafeFlowToggleBody";
    private const string CompactSafeFlowToggleTitleName = "CompactSafeFlowToggleTitle";
    private const string CompactSafeFlowToggleDetailName = "CompactSafeFlowToggleDetail";
    private const int CompactSafeFlowToggleTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactSafeFlowToggleDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactSafeFlowToggleHorizontalMargin = 6;
    private const int CompactSafeFlowToggleVerticalMargin = 4;
    private const int CompactSafeFlowGuideTitleHeight = 24;
    private const int CompactSafeFlowGuideTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactSafeFlowGuideStepHeight = 42;
    private const int CompactSafeFlowGuideStepAccentWidth = 3;
    private const int CompactSafeFlowGuideStepNumberWidth = 26;
    private const int CompactSafeFlowGuideStepNumberFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactSafeFlowGuideStepTitleFontSize = LauncherSectionMetrics.CompactDetailButtonFontSize;
    private const int CompactSafeFlowGuideStepDetailFontSize = LauncherSectionMetrics.CompactDetailLabelFontSize;
    private const int CompactSafeFlowGuideStepRadius = 6;
    private const int CompactSafeFlowGuideStepHorizontalMargin = 7;
    private const int CompactSafeFlowGuideStepVerticalMargin = 4;

    private static (
        StyledLabel StatusPhase,
        StyledLabel StatusAction,
        StyledLabel Status,
        Button CompactStatusDetailsButton,
        StyledLabel CompactStatusDetailsCue,
        ColorRect StatusAccent,
        StyledLabel[] WorkflowStepNumberLabels,
        StyledLabel[] WorkflowStepLabels,
        StyledLabel[] WorkflowStepDetailLabels,
        ColorRect[] WorkflowStepAccents,
        Button[] WorkflowStepButtons,
        GridContainer CompactStatusHeadline,
        PanelContainer CompactStatusPhasePanel,
        GridContainer CompactStickyTaskHeader,
        Control CompactWorkflowStrip,
        Button CompactCurrentTaskButton,
        ScrollContainer PrimaryScroll,
        Control FirstRunGuide,
        LoginSection Login,
        CodeSection Code,
        DownloadSection Download,
        ActionSection Actions,
        VBoxContainer CompactDiagnosticsHost
    ) BuildPrimaryColumn(LauncherLayoutProfile profile, VBoxContainer root)
    {
        var scale = profile.Scale;
        var compactCurrentTaskButton = BuildCompactCurrentTaskButton(scale, profile.Compact);
        var workflowStrip = BuildCompactWorkflowStrip(scale, profile.Compact, profile.CompactStackedActionRows);
        GridContainer compactStickyTaskHeader = null;
        if (profile.Compact)
        {
            var stickyHeader = BuildCompactStickyTaskHeader(profile, compactCurrentTaskButton, workflowStrip.Strip);
            compactStickyTaskHeader = stickyHeader.Header;
            root.AddChild(stickyHeader.Toolbar);
        }

        var leftScroll = new ScrollContainer();
        leftScroll.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftScroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.FollowFocus = true;
        root.AddChild(leftScroll);

        var leftFrame = new MarginContainer();
        leftFrame.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftFrame.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        leftScroll.AddChild(leftFrame);

        var left = new VBoxContainer();
        left.SizeFlagsHorizontal = profile.Compact
            ? Control.SizeFlags.ExpandFill
            : Control.SizeFlags.ShrinkCenter;
        left.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        left.CustomMinimumSize = new Vector2(
            profile.Compact ? 0 : profile.ContentMaxWidth,
            0
        );
        left.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                profile.Compact
                    ? LauncherViewLayoutMetrics.CompactPrimaryColumnSeparation
                    : LauncherViewLayoutMetrics.PrimaryColumnSeparation,
                scale
            )
        );
        leftFrame.AddChild(left);

        var initialPhase = LauncherPortalStatusFormatter.PhaseFor("Initializing...");
        var statusPhaseLabel = new StyledLabel(
            initialPhase,
            scale,
            fontSize: profile.Compact ? 13 : 11,
            align: HorizontalAlignment.Center
        );
        statusPhaseLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherPortalStatusFormatter.ColorFor(initialPhase)
        );

        var statusActionLabel = new StyledLabel(
            LauncherPortalStatusFormatter.ActionFor("Initializing..."),
            scale,
            fontSize: profile.Compact ? 13 : 10,
            align: HorizontalAlignment.Center
        );
        statusActionLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );

        var statusLabel = new StyledLabel(
            LauncherPortalStatusFormatter.MessageFor("Initializing..."),
            scale,
            fontSize: profile.Compact ? 15 : 14,
            align: HorizontalAlignment.Left
        );
        statusLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        var statusAccent = new ColorRect();
        statusAccent.Color = LauncherPortalStatusFormatter.ColorFor(initialPhase);
        var statusCapsule = BuildStatusCapsule(statusPhaseLabel, statusActionLabel, statusLabel, statusAccent, profile);
        left.AddChild(statusCapsule.Capsule);
        if (!profile.Compact)
            left.AddChild(workflowStrip.Strip);
        var firstRunGuide = BuildFirstRunGuide(scale, profile.Compact);
        left.AddChild(firstRunGuide);

        var login = new LoginSection(scale, profile.Compact);
        left.AddChild(login);

        var code = new CodeSection(scale, profile.Compact, profile.CompactStackedActionRows);
        left.AddChild(code);

        var download = new DownloadSection(scale, profile.Compact, profile.CompactStackedActionRows);
        left.AddChild(download);

        var actions = new ActionSection(scale, profile.Compact, profile.CompactStackedActionRows);
        left.AddChild(actions);

        VBoxContainer compactDiagnosticsHost = null;
        if (profile.Compact)
        {
            compactDiagnosticsHost = new VBoxContainer();
            compactDiagnosticsHost.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            compactDiagnosticsHost.AddThemeConstantOverride(
                LauncherViewLayoutMetrics.ThemeSeparation,
                LauncherViewLayoutMetrics.ScaleInt(
                    LauncherViewLayoutMetrics.CompactPrimaryColumnSeparation,
                    scale
                )
            );
            left.AddChild(compactDiagnosticsHost);
        }

        left.AddChild(BuildFmodAttributionSection(scale, profile.Compact));
        if (profile.Compact)
            left.AddChild(BuildCompactBottomScrollSpacer(scale));

        return (
            statusPhaseLabel,
            statusActionLabel,
            statusLabel,
            statusCapsule.CompactDetailButton,
            statusCapsule.CompactDetailCue,
            statusAccent,
            workflowStrip.StepNumberLabels,
            workflowStrip.StepLabels,
            workflowStrip.StepDetailLabels,
            workflowStrip.StepAccents,
            workflowStrip.StepButtons,
            statusCapsule.CompactHeadline,
            statusCapsule.CompactPhasePanel,
            compactStickyTaskHeader,
            workflowStrip.Strip,
            compactCurrentTaskButton,
            leftScroll,
            firstRunGuide,
            login,
            code,
            download,
            actions,
            compactDiagnosticsHost
        );
    }

    private static Button BuildCompactCurrentTaskButton(float scale, bool compact)
    {
        if (!compact)
            return new Button { Visible = false };

        var button = new StyledButton(
            "",
            scale,
            fontSize: LauncherSectionMetrics.CompactDetailButtonFontSize,
            height: LauncherSectionMetrics.CompactDetailButtonHeight
        );
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        LauncherButtonStyles.ApplySupportAction(button, scale);
        SetCompactCurrentTaskButtonText(button, scale, "Start here", "Setup guide");
        return button;
    }

    private static void SetCompactCurrentTaskButtonText(
        Button button,
        float scale,
        string title,
        string detail
    )
    {
        button.Text = "";
        var labels = EnsureCompactCurrentTaskButtonLabels(button, scale);
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private static (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) EnsureCompactCurrentTaskButtonLabels(
        Button button,
        float scale
    )
    {
        var body = button.GetNodeOrNull<VBoxContainer>(new NodePath(CompactCurrentTaskButtonBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactCurrentTaskButtonTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactCurrentTaskButtonDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactCurrentTaskButtonBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactCurrentTaskButtonHorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactCurrentTaskButtonHorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactCurrentTaskButtonVerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactCurrentTaskButtonVerticalMargin, scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );

        var title = new StyledLabel(
            "",
            scale,
            fontSize: CompactCurrentTaskButtonTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactCurrentTaskButtonTitleName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detailLabel = new StyledLabel(
            "",
            scale,
            fontSize: CompactCurrentTaskButtonDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactCurrentTaskButtonDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detailLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detailLabel);

        button.AddChild(body);
        return (body, title, detailLabel);
    }

    private static (
        Control Toolbar,
        GridContainer Header
    ) BuildCompactStickyTaskHeader(
        LauncherLayoutProfile profile,
        Button compactCurrentTaskButton,
        Control workflowStrip
    )
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

    private static void ApplyCompactStickyTaskHeaderLayout(
        GridContainer header,
        Button compactCurrentTaskButton,
        Control workflowStrip,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        var stacked = ShouldStackCompactStickyTaskHeader(profile);
        header.Columns = stacked ? 1 : 2;
        header.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                stacked ? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap,
                scale
            )
        );

        if (stacked)
        {
            compactCurrentTaskButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            compactCurrentTaskButton.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactStackedCurrentTaskHeight, scale)
            );
            workflowStrip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            workflowStrip.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
            return;
        }

        compactCurrentTaskButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        compactCurrentTaskButton.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskButtonMinWidth, scale),
            LauncherViewLayoutMetrics.ScaleInt(CompactInlineCurrentTaskHeight, scale)
        );
        workflowStrip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        workflowStrip.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    }

    private static Control WrapCompactStickyTaskHeader(float scale, Control header)
    {
        var toolbar = new PanelContainer();
        toolbar.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolbar.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildCompactStickyTaskHeaderStyle(scale)
        );
        header.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        toolbar.AddChild(header);
        return toolbar;
    }

    private static StyleBoxFlat BuildCompactStickyTaskHeaderStyle(float scale)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.018f, 0.035f, 0.045f, 0.9f),
            LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarRadius, scale)
        );
        style.BorderColor = new Color(0.04f, 0.42f, 0.5f, 0.45f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactStickyTaskToolbarVerticalMargin, scale);
        return style;
    }

    private static bool ShouldStackCompactStickyTaskHeader(LauncherLayoutProfile profile)
        => profile.ContentMaxWidth < LauncherViewLayoutMetrics.ScaleInt(
            CompactStickyTaskHeaderStackWidth,
            profile.Scale
        );

    private static (
        Control Strip,
        StyledLabel[] StepNumberLabels,
        StyledLabel[] StepLabels,
        StyledLabel[] StepDetailLabels,
        ColorRect[] StepAccents,
        Button[] StepButtons
    ) BuildCompactWorkflowStrip(
        float scale,
        bool compact,
        bool denseNarrowWorkflow
    )
    {
        if (!compact)
            return (
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
            var cell = BuildCompactWorkflowStepButton(i, scale, stepHeight);
            buttons[i] = cell;

            var body = new VBoxContainer();
            body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            body.MouseFilter = Control.MouseFilterEnum.Ignore;
            body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
            body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepHorizontalMargin, scale);
            body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
            body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepVerticalMargin, scale);
            body.AddThemeConstantOverride(
                LauncherViewLayoutMetrics.ThemeSeparation,
                LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepSeparation, scale)
            );
            cell.AddChild(body);

            var labelRow = new HBoxContainer();
            labelRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            labelRow.MouseFilter = Control.MouseFilterEnum.Ignore;
            labelRow.AddThemeConstantOverride(
                LauncherViewLayoutMetrics.ThemeSeparation,
                LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepNumberGap, scale)
            );
            body.AddChild(labelRow);

            var numberLabel = new StyledLabel(
                CompactWorkflowStepNumbers[i],
                scale,
                fontSize: CompactWorkflowStepNumberFontSize,
                align: HorizontalAlignment.Center
            );
            numberLabel.CustomMinimumSize = new Vector2(
                LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepNumberMinWidth, scale),
                0
            );
            numberLabel.VerticalAlignment = VerticalAlignment.Center;
            numberLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
            numberLabel.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                LauncherComponentTheme.TextMuted
            );
            numberLabels[i] = numberLabel;
            labelRow.AddChild(numberLabel);

            var label = new StyledLabel(
                CompactWorkflowStepNames[i],
                scale,
                fontSize: CompactWorkflowStepLabelFontSize,
                align: HorizontalAlignment.Left
            );
            label.VerticalAlignment = VerticalAlignment.Center;
            label.MouseFilter = Control.MouseFilterEnum.Ignore;
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            label.ClipText = true;
            label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            label.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                LauncherComponentTheme.TextMuted
            );
            labels[i] = label;
            labelRow.AddChild(label);

            var detail = new StyledLabel(
                CompactWorkflowStepDetails[i],
                scale,
                fontSize: CompactWorkflowStepDetailFontSize,
                align: HorizontalAlignment.Center
            );
            detail.VerticalAlignment = VerticalAlignment.Center;
            detail.MouseFilter = Control.MouseFilterEnum.Ignore;
            detail.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            detail.ClipText = true;
            detail.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            detail.AddThemeColorOverride(
                LauncherViewLayoutMetrics.ThemeFontColor,
                LauncherComponentTheme.TextMuted
            );
            detailLabels[i] = detail;
            body.AddChild(detail);

            var accent = new ColorRect
            {
                Color = LauncherComponentTheme.ButtonNormal,
                CustomMinimumSize = new Vector2(
                    0,
                    LauncherViewLayoutMetrics.ScaleInt(CompactWorkflowStepAccentHeight, scale)
                ),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            accents[i] = accent;
            body.AddChild(accent);
            grid.AddChild(cell);
        }

        return (grid, numberLabels, labels, detailLabels, accents, buttons);
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

    private static Control BuildCompactBottomScrollSpacer(float scale)
        => new Control
        {
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(72, scale)
            ),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };

    private static (
        Control Capsule,
        GridContainer CompactHeadline,
        PanelContainer CompactPhasePanel,
        Button CompactDetailButton,
        StyledLabel CompactDetailCue
    ) BuildStatusCapsule(
        StyledLabel statusPhaseLabel,
        StyledLabel statusActionLabel,
        StyledLabel statusLabel,
        ColorRect statusAccent,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        if (profile.Compact)
            return BuildCompactStatusCapsule(statusPhaseLabel, statusActionLabel, statusLabel, statusAccent, profile);

        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusStyle(scale, compact: false)
        );

        var body = new HBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        panel.AddChild(body);

        statusAccent.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(4, scale),
            LauncherViewLayoutMetrics.ScaleInt(30, scale)
        );
        body.AddChild(statusAccent);

        var phasePanel = new PanelContainer();
        phasePanel.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(96, scale),
            0
        );
        phasePanel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusPhaseStyle(scale, compact: false)
        );
        var phaseBody = new VBoxContainer();
        phaseBody.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        phaseBody.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(1, scale)
        );
        statusPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
        phaseBody.AddChild(statusPhaseLabel);
        phaseBody.AddChild(statusActionLabel);
        phasePanel.AddChild(phaseBody);
        body.AddChild(phasePanel);

        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddChild(statusLabel);
        return (panel, null, null, null, null);
    }

    private static (
        Control Capsule,
        GridContainer CompactHeadline,
        PanelContainer CompactPhasePanel,
        Button CompactDetailButton,
        StyledLabel CompactDetailCue
    ) BuildCompactStatusCapsule(
        StyledLabel statusPhaseLabel,
        StyledLabel statusActionLabel,
        StyledLabel statusLabel,
        ColorRect statusAccent,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusStyle(scale, compact: true)
        );

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusBodySeparation, scale)
        );
        panel.AddChild(body);

        statusAccent.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusAccentHeight, scale)
        );
        body.AddChild(statusAccent);

        var headline = new GridContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };

        var phasePanel = new PanelContainer();
        phasePanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        phasePanel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildStatusPhaseStyle(scale, compact: true)
        );
        statusPhaseLabel.VerticalAlignment = VerticalAlignment.Center;
        phasePanel.AddChild(statusPhaseLabel);
        headline.AddChild(phasePanel);

        statusActionLabel.HorizontalAlignment = HorizontalAlignment.Left;
        statusActionLabel.VerticalAlignment = VerticalAlignment.Center;
        statusActionLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        statusActionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusActionLabel.ClipText = false;
        headline.AddChild(statusActionLabel);
        ApplyCompactStatusHeadlineLayout(headline, phasePanel, statusActionLabel, profile);
        body.AddChild(headline);

        var detailButton = BuildCompactStatusDetailButton(scale);
        var detailRow = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        detailRow.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        detailRow.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        detailRow.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        detailRow.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        detailRow.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        detailRow.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailRowGap, scale)
        );

        statusLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusLabel.HorizontalAlignment = HorizontalAlignment.Left;
        statusLabel.VerticalAlignment = VerticalAlignment.Center;
        statusLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        statusLabel.AutowrapMode = TextServer.AutowrapMode.Off;
        statusLabel.ClipText = true;
        statusLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        detailRow.AddChild(statusLabel);

        var detailCue = new StyledLabel(
            "Details",
            scale,
            fontSize: CompactStatusDetailCueFontSize,
            align: HorizontalAlignment.Center
        );
        detailCue.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailCueWidth, scale),
            0
        );
        detailCue.VerticalAlignment = VerticalAlignment.Center;
        detailCue.MouseFilter = Control.MouseFilterEnum.Ignore;
        detailCue.ClipText = true;
        detailCue.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        detailCue.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        detailRow.AddChild(detailCue);

        detailButton.AddChild(detailRow);
        body.AddChild(detailButton);

        return (panel, headline, phasePanel, detailButton, detailCue);
    }

    private static Button BuildCompactStatusDetailButton(float scale)
    {
        var button = new Button
        {
            Text = "",
            ClipText = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = "Show full launcher status",
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHeight, scale)
            ),
        };
        ApplyCompactStatusDetailButtonStyle(button, scale);
        return button;
    }

    private static void ApplyCompactStatusDetailButtonStyle(Button button, float scale)
    {
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateNormal,
            BuildCompactStatusDetailButtonStyle(
                scale,
                new Color(0.025f, 0.045f, 0.06f, 0.76f),
                new Color(0.05f, 0.34f, 0.42f, 0.4f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateHover,
            BuildCompactStatusDetailButtonStyle(
                scale,
                new Color(0.035f, 0.075f, 0.095f, 0.86f),
                new Color(0.06f, 0.54f, 0.62f, 0.58f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StatePressed,
            BuildCompactStatusDetailButtonStyle(
                scale,
                new Color(0.02f, 0.035f, 0.05f, 0.94f),
                new Color(0.95f, 0.42f, 0.08f, 0.68f)
            )
        );
        button.AddThemeStyleboxOverride(
            LauncherComponentTheme.StateDisabled,
            BuildCompactStatusDetailButtonStyle(
                scale,
                new Color(0.025f, 0.035f, 0.045f, 0.48f),
                new Color(0.05f, 0.16f, 0.2f, 0.24f)
            )
        );
    }

    private static StyleBoxFlat BuildCompactStatusDetailButtonStyle(float scale, Color body, Color border)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            body,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailRadius, scale)
        );
        style.BorderColor = border;
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailHorizontalMargin, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(CompactStatusDetailVerticalMargin, scale);
        return style;
    }

    private static void ApplyCompactStatusHeadlineLayout(
        GridContainer headline,
        PanelContainer phasePanel,
        StyledLabel statusActionLabel,
        LauncherLayoutProfile profile
    )
    {
        var scale = profile.Scale;
        var stacked = profile.CompactStackedActionRows;
        headline.Columns = stacked ? 1 : 2;
        headline.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(
                stacked ? CompactStatusHeadlineSeparation : CompactStatusHeadlineInlineSeparation,
                scale
            )
        );
        phasePanel.CustomMinimumSize = new Vector2(
            LauncherViewLayoutMetrics.ScaleInt(stacked ? 0 : CompactStatusPhaseInlineWidth, scale),
            0
        );
        phasePanel.SizeFlagsHorizontal = stacked
            ? Control.SizeFlags.ExpandFill
            : Control.SizeFlags.ShrinkBegin;
        statusActionLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        statusActionLabel.CustomMinimumSize = new Vector2(
            0,
            LauncherViewLayoutMetrics.ScaleInt(CompactStatusActionMinHeight, scale)
        );
    }

    private static StyleBoxFlat BuildStatusStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.02f, 0.04f, 0.06f, 0.92f),
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        style.BorderColor = new Color(0.05f, 0.5f, 0.58f, 0.7f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 7 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 8, scale);
        return style;
    }

    private static StyleBoxFlat BuildStatusPhaseStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.045f, 0.075f, 0.095f, 0.95f),
            LauncherViewLayoutMetrics.ScaleInt(7, scale)
        );
        style.BorderColor = new Color(0.08f, 0.36f, 0.42f, 0.65f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseHorizontalMargin : 8, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseHorizontalMargin : 8, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseVerticalMargin : 5, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? CompactStatusPhaseVerticalMargin : 5, scale);
        return style;
    }

    private static Control BuildFirstRunGuide(float scale, bool compact)
    {
        if (compact)
            return BuildCollapsedFirstRunGuide(scale);

        return BuildFirstRunGuidePanel(scale, compact: false);
    }

    private static Control BuildCollapsedFirstRunGuide(float scale)
    {
        var wrapper = new VBoxContainer();
        wrapper.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        wrapper.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(5, scale)
        );

        var toggle = new StyledButton(
            "",
            scale,
            fontSize: LauncherSectionMetrics.CompactDetailButtonFontSize,
            height: LauncherSectionMetrics.CompactDrawerToggleHeight
        );
        LauncherButtonStyles.ApplySupportAction(toggle, scale);
        SetCompactSafeFlowToggleText(toggle, scale, "Quick Start", "Get saves first");
        wrapper.AddChild(toggle);

        var guide = BuildFirstRunGuidePanel(scale, compact: true);
        guide.Visible = false;
        wrapper.AddChild(guide);

        toggle.Pressed += () =>
        {
            guide.Visible = !guide.Visible;
            if (guide.Visible)
                SetCompactSafeFlowToggleText(toggle, scale, "Hide Guide", "Safe order");
            else
                SetCompactSafeFlowToggleText(toggle, scale, "Quick Start", "Get saves first");
        };

        return wrapper;
    }

    private static void SetCompactSafeFlowToggleText(
        Button toggle,
        float scale,
        string title,
        string detail
    )
    {
        toggle.Text = "";
        var labels = EnsureCompactSafeFlowToggleLabels(toggle, scale);
        labels.Body.Visible = true;
        labels.Title.Text = title;
        labels.Detail.Text = detail;
    }

    private static (VBoxContainer Body, StyledLabel Title, StyledLabel Detail) EnsureCompactSafeFlowToggleLabels(
        Button toggle,
        float scale
    )
    {
        var body = toggle.GetNodeOrNull<VBoxContainer>(new NodePath(CompactSafeFlowToggleBodyName));
        if (body != null)
        {
            return (
                body,
                body.GetNode<StyledLabel>(new NodePath(CompactSafeFlowToggleTitleName)),
                body.GetNode<StyledLabel>(new NodePath(CompactSafeFlowToggleDetailName))
            );
        }

        body = new VBoxContainer
        {
            Name = CompactSafeFlowToggleBodyName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        body.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        body.OffsetLeft = LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowToggleHorizontalMargin, scale);
        body.OffsetRight = -LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowToggleHorizontalMargin, scale);
        body.OffsetTop = LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowToggleVerticalMargin, scale);
        body.OffsetBottom = -LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowToggleVerticalMargin, scale);
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            0
        );

        var title = new StyledLabel(
            "",
            scale,
            fontSize: CompactSafeFlowToggleTitleFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactSafeFlowToggleTitleName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        body.AddChild(title);

        var detailLabel = new StyledLabel(
            "",
            scale,
            fontSize: CompactSafeFlowToggleDetailFontSize,
            align: HorizontalAlignment.Center
        )
        {
            Name = CompactSafeFlowToggleDetailName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
            ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
        };
        detailLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(detailLabel);

        toggle.AddChild(body);
        return (body, title, detailLabel);
    }

    private static Control BuildFirstRunGuidePanel(float scale, bool compact)
    {
        var panel = new PanelContainer();
        panel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildFirstRunGuideStyle(scale, compact)
        );

        var body = new VBoxContainer();
        body.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        body.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(5, scale)
        );
        panel.AddChild(body);

        var title = new StyledLabel(
            "Quick start guide",
            scale,
            fontSize: compact ? CompactSafeFlowGuideTitleFontSize : 12,
            align: HorizontalAlignment.Left
        );
        if (compact)
        {
            title.AutowrapMode = TextServer.AutowrapMode.Off;
            title.ClipText = true;
            title.VerticalAlignment = VerticalAlignment.Center;
            title.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            title.CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideTitleHeight, scale)
            );
        }
        title.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.CyanAccent
        );
        body.AddChild(title);

        if (compact)
        {
            AddCompactSafeFlowSteps(body, scale);
            return panel;
        }

        var guidance = new StyledLabel(
            "Sign in, choose a game version, get Steam saves, then start the game. Upload stays locked until you deliberately open it after checking local saves.",
            scale,
            fontSize: 11,
            align: HorizontalAlignment.Left
        );
        guidance.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        guidance.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        body.AddChild(guidance);

        return panel;
    }

    private static void AddCompactSafeFlowSteps(VBoxContainer body, float scale)
    {
        body.AddChild(BuildCompactSafeFlowStep(
            scale,
            "1",
            "Sign in",
            "Steam account",
            LauncherComponentTheme.OrangeAccent
        ));
        body.AddChild(BuildCompactSafeFlowStep(
            scale,
            "2",
            "Get files",
            "Version on Android",
            LauncherComponentTheme.CyanAccent
        ));
        body.AddChild(BuildCompactSafeFlowStep(
            scale,
            "3",
            "Get saves",
            "Steam to Android",
            LauncherComponentTheme.CyanAccent
        ));
        body.AddChild(BuildCompactSafeFlowStep(
            scale,
            "4",
            "Play",
            "Ready version",
            LauncherComponentTheme.OrangeHot
        ));
        body.AddChild(BuildCompactSafeFlowStep(
            scale,
            "5",
            "Upload locked",
            "Review before uploading",
            LauncherComponentTheme.TextMuted
        ));
    }

    private static Control BuildCompactSafeFlowStep(
        float scale,
        string marker,
        string title,
        string detail,
        Color accent
    )
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(
                0,
                LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepHeight, scale)
            ),
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        panel.AddThemeStyleboxOverride(
            LauncherComponentTheme.Panel,
            BuildCompactSafeFlowStepStyle(scale, accent)
        );

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        row.AddThemeConstantOverride(
            LauncherViewLayoutMetrics.ThemeSeparation,
            LauncherViewLayoutMetrics.ScaleInt(7, scale)
        );

        var accentLine = new ColorRect
        {
            Color = accent,
            CustomMinimumSize = new Vector2(
                LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepAccentWidth, scale),
                0
            ),
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        row.AddChild(accentLine);

        var markerLabel = new StyledLabel(
            marker,
            scale,
            fontSize: CompactSafeFlowGuideStepNumberFontSize,
            align: HorizontalAlignment.Center
        )
        {
            CustomMinimumSize = new Vector2(
                LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepNumberWidth, scale),
                0
            ),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            VerticalAlignment = VerticalAlignment.Center,
        };
        markerLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            accent
        );
        row.AddChild(markerLabel);

        var textColumn = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        textColumn.AddThemeConstantOverride(LauncherViewLayoutMetrics.ThemeSeparation, 0);

        var titleLabel = new StyledLabel(
            title,
            scale,
            fontSize: CompactSafeFlowGuideStepTitleFontSize,
            align: HorizontalAlignment.Left
        )
        {
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titleLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextPrimary
        );
        textColumn.AddChild(titleLabel);

        var detailLabel = new StyledLabel(
            detail,
            scale,
            fontSize: CompactSafeFlowGuideStepDetailFontSize,
            align: HorizontalAlignment.Left
        )
        {
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            TooltipText = detail,
            VerticalAlignment = VerticalAlignment.Center,
        };
        detailLabel.AddThemeColorOverride(
            LauncherViewLayoutMetrics.ThemeFontColor,
            LauncherComponentTheme.TextSecondary
        );
        textColumn.AddChild(detailLabel);
        row.AddChild(textColumn);
        panel.AddChild(row);
        return panel;
    }

    private static StyleBoxFlat BuildCompactSafeFlowStepStyle(float scale, Color accent)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.03f, 0.055f, 0.07f, 0.88f),
            LauncherViewLayoutMetrics.ScaleInt(CompactSafeFlowGuideStepRadius, scale)
        );
        style.BorderColor = new Color(accent.R, accent.G, accent.B, 0.3f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(
            CompactSafeFlowGuideStepHorizontalMargin,
            scale
        );
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(
            CompactSafeFlowGuideStepHorizontalMargin,
            scale
        );
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(
            CompactSafeFlowGuideStepVerticalMargin,
            scale
        );
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(
            CompactSafeFlowGuideStepVerticalMargin,
            scale
        );
        return style;
    }

    private static StyleBoxFlat BuildFirstRunGuideStyle(float scale, bool compact)
    {
        var style = LauncherStyleBoxes.MakeFilled(
            new Color(0.025f, 0.045f, 0.06f, 0.88f),
            LauncherViewLayoutMetrics.ScaleInt(8, scale)
        );
        style.BorderColor = new Color(0.05f, 0.5f, 0.58f, 0.35f);
        style.SetBorderWidthAll(Math.Max(1, LauncherViewLayoutMetrics.ScaleInt(1, scale)));
        style.ContentMarginLeft = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginRight = LauncherViewLayoutMetrics.ScaleInt(compact ? 8 : 10, scale);
        style.ContentMarginTop = LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale);
        style.ContentMarginBottom = LauncherViewLayoutMetrics.ScaleInt(compact ? 6 : 8, scale);
        return style;
    }
}
