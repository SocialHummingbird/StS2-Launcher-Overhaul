function Add-SteamVersionSelectionCompactWorkflowStripShellChecks {
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
}
