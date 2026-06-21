function Add-SteamVersionSelectionCompactWorkflowStickyHeaderPlacementChecks {
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
}
