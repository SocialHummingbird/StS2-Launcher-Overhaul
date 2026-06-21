function Add-SteamVersionSelectionCompactWorkflowChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.cs" `
        "orchestrates the touch-safe responsive compact workflow step strip" `
        @(
            "BuildCompactWorkflowStrip",
            "CompactWorkflowStepHeight = LauncherSectionMetrics\.CompactDetailButtonHeight",
            "CompactWorkflowStepDenseHeight = LauncherSectionMetrics\.CompactDetailButtonHeight",
            "CompactWorkflowStepLabelFontSize = 13",
            "CompactWorkflowStepDetailFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
            "CompactWorkflowStepNumberFontSize = LauncherSectionMetrics\.CompactDetailLabelFontSize",
            "CompactWorkflowStepNumberMinWidth = 20",
            "CompactWorkflowStepAccentHeight = 2",
            "CompactWorkflowStepSeparation = 0",
            "CompactWorkflowStepCellGap = 3",
            "CompactWorkflowStepNumberGap = 3",
            "CompactWorkflowStepHorizontalMargin = 5",
            "CompactWorkflowStepVerticalMargin = 4",
            "GridContainer",
            "bool denseNarrowWorkflow",
            "Columns = CompactWorkflowStepNames\.Length",
            "var stepHeight = denseNarrowWorkflow",
            "\? CompactWorkflowStepDenseHeight",
            ": CompactWorkflowStepHeight",
            "new LauncherViewCompactWorkflowStrip",
            "BuildCompactWorkflowStepCell",
            "numberLabels\[i\] = cell\.NumberLabel",
            "labels\[i\] = cell\.Label",
            "detailLabels\[i\] = cell\.DetailLabel",
            "accents\[i\] = cell\.Accent",
            "buttons\[i\] = cell\.Button",
            "grid\.AddChild\(cell\.Button\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Result.cs" `
        "uses typed compact workflow strip and step-cell layout results instead of out-parameter construction" `
        @(
            "LauncherViewCompactWorkflowStrip",
            "LauncherViewCompactWorkflowStepCell",
            "StepNumberLabels",
            "StepLabels",
            "StepDetailLabels",
            "StepAccents",
            "StepButtons",
            "NumberLabel",
            "DetailLabel",
            "ColorRect Accent"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Cells.cs" `
        "builds compact workflow step cells from typed button, label, detail, and accent parts" `
        @(
            "BuildCompactWorkflowStepCell",
            "BuildCompactWorkflowStepButton",
            "BuildCompactWorkflowStepBody",
            "BuildCompactWorkflowLabelRow",
            "BuildCompactWorkflowNumberLabel",
            "BuildCompactWorkflowLabel",
            "BuildCompactWorkflowDetailLabel",
            "BuildCompactWorkflowAccent",
            "new LauncherViewCompactWorkflowStepCell",
            "button\.AddChild\(body\)",
            "labelRow\.AddChild\(numberLabel\)",
            "body\.AddChild\(detail\)",
            "body\.AddChild\(accent\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Cells.Body.cs" `
        "builds compact workflow cell body, label row, and accent layout chrome" `
        @(
            "BuildCompactWorkflowStepBody",
            "BuildCompactWorkflowLabelRow",
            "BuildCompactWorkflowAccent",
            "SetAnchorsPreset\(Control\.LayoutPreset\.FullRect\)",
            "new HBoxContainer",
            "OffsetLeft",
            "OffsetRight",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactWorkflowStepAccentHeight, scale\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Cells.Labels.cs" `
        "builds compact workflow number, title, and detail labels without hover-only hints" `
        @(
            "BuildCompactWorkflowNumberLabel",
            "BuildCompactWorkflowLabel",
            "BuildCompactWorkflowDetailLabel",
            "CompactWorkflowStepNumbers\[index\]",
            "CompactWorkflowStepNames\[index\]",
            "CompactWorkflowStepDetails\[index\]",
            "label\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "label\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "detail\.TextOverrunBehavior = TextServer\.OverrunBehavior\.TrimEllipsis",
            "fontSize: CompactWorkflowStepNumberFontSize",
            "fontSize: CompactWorkflowStepLabelFontSize",
            "fontSize: CompactWorkflowStepDetailFontSize"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactWorkflow.Style.cs" `
        "keeps compact workflow step buttons touch-targeted and state-styled" `
        @(
            "BuildCompactWorkflowStepButton\(int index, float scale, int height\)",
            "ApplyWorkflowStepButtonStyle",
            "CompactWorkflowStepTooltips",
            "MouseDefaultCursorShape = Control\.CursorShape\.PointingHand",
            "Go to \{CompactWorkflowStepTooltips\[index\]\}",
            "LauncherViewLayoutMetrics\.ScaleInt\(height, scale\)",
            "LauncherComponentTheme\.StateNormal",
            "LauncherComponentTheme\.StateHover",
            "LauncherComponentTheme\.StatePressed",
            "LauncherComponentTheme\.StateDisabled",
            "BuildWorkflowStepStyle"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.Navigation.cs" `
        "makes compact workflow steps tappable direct navigation controls" `
        @(
            "_workflowStepButtons",
            "WireCompactWorkflowStepNavigation",
            "ScrollCompactWorkflowStep",
            "Pressed \+= \(\) => ScrollCompactWorkflowStep\(capturedStep\)",
            "CompactWorkflowStep\.SignIn => Login\.Visible",
            "CompactWorkflowStep\.Code => Code\.Visible",
            "CompactWorkflowStep\.Files => Download\.Visible",
            "CompactWorkflowStep\.Play => _compactCurrentTaskTarget",
            "ScrollCompactPrimaryTo\(target\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
        "anchors the compact workflow step strip outside the scrolling body so progress remains visible" `
        @(
            "var workflowStrip = BuildCompactWorkflowStrip\(scale, profile\.Compact, profile\.CompactStackedActionRows\)",
            "BuildCompactStickyTaskHeader\(profile, compactCurrentTaskButton, workflowStrip\.Strip\)",
            "BuildPrimaryColumnBody\(profile, root\)",
            "if \(!profile\.Compact\)",
            "left\.AddChild\(workflowStrip\.Strip\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.Button.cs" `
        "adds a low-profile compact current-task jump button through shared two-line labels" `
        @(
            "CompactCurrentTaskButtonLabels",
            "CompactButtonDetailLabelSpec",
            "BuildCompactCurrentTaskButton",
            "SetCompactCurrentTaskButtonText",
            "CompactCurrentTaskButtonBodyName",
            "CompactCurrentTaskButtonTitleName",
            "CompactCurrentTaskButtonDetailName",
            "CompactButtonDetailLabelSpec\.Default",
            "CompactButtonDetailLabels\.Apply",
            "enabled: true",
            'SetCompactCurrentTaskButtonText\(button, scale, "Start here", "Setup guide"\)',
            "LauncherSectionMetrics\.CompactDetailButtonHeight",
            "LauncherSectionMetrics\.CompactDetailButtonFontSize",
            "LauncherButtonStyles\.ApplySupportAction",
            "compactCurrentTaskButton"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.cs" `
        "anchors the compact current-task jump button outside the scrolling body so it remains reachable" `
        @(
            "var compactCurrentTaskButton = BuildCompactCurrentTaskButton\(scale, profile\.Compact\)",
            "if \(profile\.Compact\)",
            "BuildCompactStickyTaskHeader\(profile, compactCurrentTaskButton, workflowStrip\.Strip\)",
            "BuildPrimaryColumnBody\(profile, root\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Body.cs" `
        "builds the primary scroll container and centered body after compact sticky chrome" `
        @(
            "BuildPrimaryColumnBody",
            "new ScrollContainer",
            "leftScroll\.FollowFocus = true",
            "root\.AddChild\(leftScroll\)",
            "new MarginContainer",
            "leftFrame\.AddChild\(left\)",
            "LauncherViewLayoutMetrics\.CompactPrimaryColumnSeparation",
            "LauncherViewLayoutMetrics\.PrimaryColumnSeparation",
            "return new LauncherViewPrimaryBody"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.cs" `
        "builds the compact current-task button and workflow strip as one viewport-reflowable sticky header grid" `
        @(
            "CompactStickyTaskHeaderInlineGap = 6",
            "CompactStickyTaskHeaderStackGap = 3",
            "CompactStickyTaskButtonMinWidth = 176",
            "CompactInlineCurrentTaskHeight = LauncherSectionMetrics\.CompactDetailButtonHeight",
            "CompactStackedCurrentTaskHeight = CompactWorkflowStepDenseHeight",
            "CompactStickyTaskHeaderStackWidth = 560",
            "CompactStickyTaskHeaderGridName",
            "CompactStickyTaskToolbarRadius = 7",
            "CompactStickyTaskToolbarHorizontalMargin = 5",
            "CompactStickyTaskToolbarVerticalMargin = 4",
            "GridContainer Header",
            "Control workflowStrip",
            "return \(WrapCompactStickyTaskHeader\(scale, header\), header\)",
            "BuildCompactStickyTaskHeader",
            "new GridContainer",
            "Name = CompactStickyTaskHeaderGridName",
            "ApplyCompactStickyTaskHeaderLayout",
            "header\.AddChild\(compactCurrentTaskButton\)",
            "header\.AddChild\(workflowStrip\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.Style.cs" `
        "wraps the compact sticky task header in a low-profile toolbar shell" `
        @(
            "WrapCompactStickyTaskHeader",
            "BuildCompactStickyTaskHeaderStyle",
            "new PanelContainer",
            "BuildCompactStickyTaskHeaderStyle\(scale\)",
            "LauncherComponentTheme\.Panel",
            "LauncherStyleBoxes\.MakeFilled",
            "CompactStickyTaskToolbarRadius",
            "SetBorderWidthAll",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskToolbarHorizontalMargin, scale\)",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskToolbarVerticalMargin, scale\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.CompactTaskHeader.Layout.cs" `
        "stacks the compact sticky task header on narrow compact viewports so task and workflow controls stay readable" `
        @(
            "ShouldStackCompactStickyTaskHeader",
            "profile\.ContentMaxWidth < LauncherViewLayoutMetrics\.ScaleInt",
            "ApplyCompactStickyTaskHeaderLayout",
            "header\.Columns = stacked \? 1 : 2",
            "stacked \? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap",
            "stacked \? CompactStickyTaskHeaderStackGap : CompactStickyTaskHeaderInlineGap",
            "compactCurrentTaskButton\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStackedCurrentTaskHeight, scale\)",
            "workflowStrip\.SizeFlagsVertical = Control\.SizeFlags\.ShrinkBegin",
            "compactCurrentTaskButton\.SizeFlagsHorizontal = Control\.SizeFlags\.ShrinkBegin",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactStickyTaskButtonMinWidth, scale\)",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactInlineCurrentTaskHeight, scale\)",
            "workflowStrip\.SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Responsive.cs" `
        "reflows the compact sticky task header after Android viewport changes" `
        @(
            "UpdateCompactStickyTaskHeader\(Vector2 viewportSize\)",
            "LauncherLayoutProfile\.ForViewport\(viewportSize\)",
            "ApplyCompactStickyTaskHeaderLayout",
            "_compactStickyTaskHeader",
            "_compactWorkflowStrip"
    )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.Data.cs" `
        "defines compact workflow step labels, details, and navigation enum" `
        @(
            "CompactWorkflowStepNames",
            "CompactWorkflowStepNumbers",
            "CompactWorkflowStepDetails",
            "CompactWorkflowStep",
            '"Sign in"',
            '"Verify"',
            '"Files"',
            '"Play"',
            '"1"',
            '"2"',
            '"3"',
            '"4"',
            "CompactWorkflowStepTooltips",
            "Sign in",
            "Steam Guard",
            "Game files",
            "Saves safe",
            "Open sign-in",
            "Open Steam Guard",
            "Open game files",
            "Open play and saves"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.State.cs" `
        "updates compact workflow active/completed step colors" `
        @(
            "SetCompactWorkflowStep",
            "_workflowStepNumberLabels",
            "_workflowStepNumberLabels\[i\]\.AddThemeColorOverride",
            "_workflowStepDetailLabels\[i\]\.AddThemeColorOverride",
            "LauncherComponentTheme\.OrangeHot",
            "LauncherComponentTheme\.CyanAccent",
            "LauncherComponentTheme\.CyanDim",
            "LauncherComponentTheme\.TextMuted"
    )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.Navigation.cs" `
        "wires the compact current-task jump button without invoking launcher actions directly" `
        @(
            "_compactCurrentTaskButton",
            "WireCompactCurrentTaskNavigation",
            "ScrollCompactPrimaryTo\(_compactCurrentTaskTarget\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.State.cs" `
        "tracks the compact current-task jump target without invoking launcher actions directly" `
        @(
            "_compactCurrentTaskButton",
            "_compactCurrentTaskTarget",
            "SetCompactCurrentTask",
            "SetCompactCurrentTaskButtonText",
            "string detail",
            "SetCompactCurrentTaskButtonText\(_compactCurrentTaskButton, _scale, text, detail\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "updates compact workflow steps during auth section transitions" `
        @(
            "SetCompactWorkflowStep\(CompactWorkflowStep\.SignIn\)",
            "SetCompactWorkflowStep\(CompactWorkflowStep\.Code\)",
            "SetLoginFormVisible",
            "ShowCodePrompt"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "updates compact workflow steps during download section transitions" `
        @(
            "SetCompactWorkflowStep\(CompactWorkflowStep\.Files\)",
            "ShowDownloadProgress",
            "SetDownloadProgress"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "updates compact workflow steps during play and retry section transitions" `
        @(
            "SetCompactWorkflowStep\(CompactWorkflowStep\.SignIn\)",
            "SetCompactWorkflowStep\(CompactWorkflowStep\.Play\)",
            "ShowLaunchActions",
            "ShowRetry"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "updates compact current-task jump button during auth transitions" `
        @(
            'SetCompactCurrentTask\("Sign in", Login, "Steam account"\)',
            'SetCompactCurrentTask\("Verify", Code, "Steam Guard code"\)'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "updates compact current-task jump button during download transitions" `
        @(
            'SetCompactCurrentTask\("Files", Download, "Download version"\)',
            "ShowDownloadAction",
            "ShowDownloadProgress"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "updates compact current-task jump button during play and retry transitions" `
        @(
            'SetCompactCurrentTask\("Retry", Actions\.RetryScrollTarget, "Restart safely"\)',
            'SetCompactCurrentTask\("Play", Actions\.ReadyScrollTarget, "Play and saves"\)'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "labels compact auth current-task jumps as navigation rather than direct launcher actions" `
        @(
            '"Sign in"',
            '"Verify"'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "labels compact download current-task jumps as navigation rather than direct launcher actions" `
        @(
            '"Files"',
            "Download version"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "labels compact play current-task jumps as navigation rather than direct launcher actions" `
        @(
            '"Retry"',
            '"Play"'
        )
}
