function Add-SteamVersionSelectionPortalActionCompactWorkflowStripBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.strip.ps1" `
        "keeps compact workflow strip and step-cell audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStripChecks",
            "audit-steam-version-selection.compact-workflow.strip.shell.ps1",
            "audit-steam-version-selection.compact-workflow.strip.cells.ps1",
            "audit-steam-version-selection.compact-workflow.strip.style-navigation.ps1",
            "Add-SteamVersionSelectionCompactWorkflowStripShellChecks",
            "Add-SteamVersionSelectionCompactWorkflowStripCellChecks",
            "Add-SteamVersionSelectionCompactWorkflowStripStyleNavigationChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.strip.shell.ps1" `
        "keeps compact workflow strip shell audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStripShellChecks",
            "LauncherView.Layout.CompactWorkflow.cs",
            "LauncherView.Layout.CompactWorkflow.Result.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.strip.cells.ps1" `
        "keeps compact workflow strip cell composition audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStripCellChecks",
            "LauncherView.Layout.CompactWorkflow.Cells.cs",
            "LauncherView.Layout.CompactWorkflow.Cells.Body.cs",
            "LauncherView.Layout.CompactWorkflow.Cells.Labels.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.strip.style-navigation.ps1" `
        "keeps compact workflow strip style and navigation audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStripStyleNavigationChecks",
            "LauncherView.Layout.CompactWorkflow.Style.cs",
            "LauncherView.CompactWorkflow.Navigation.cs"
        )
}
