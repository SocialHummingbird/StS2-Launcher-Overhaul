function Add-SteamVersionSelectionCompactWorkflowStickyHeaderLayoutChecks {
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
}
