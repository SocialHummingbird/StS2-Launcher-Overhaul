function Add-SteamVersionSelectionCompactWorkflowStripCellChecks {
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
}
