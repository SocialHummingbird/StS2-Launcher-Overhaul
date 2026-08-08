function Add-SteamVersionSelectionCompactWorkflowStripStyleNavigationChecks {
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
}
