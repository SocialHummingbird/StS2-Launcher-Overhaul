function Add-SteamVersionSelectionCompactWorkflowStickyHeaderButtonChecks {
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
}
