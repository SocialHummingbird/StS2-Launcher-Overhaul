function Add-SteamVersionSelectionPortalActionCompactWorkflowShellBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.ps1" `
        "keeps compact workflow and current-task audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowChecks",
            "audit-steam-version-selection.compact-workflow.strip.ps1",
            "audit-steam-version-selection.compact-workflow.sticky-header.ps1",
            "audit-steam-version-selection.compact-workflow.state.ps1",
            "audit-steam-version-selection.compact-workflow.sections.ps1",
            "Add-SteamVersionSelectionCompactWorkflowStripChecks",
            "Add-SteamVersionSelectionCompactWorkflowStickyHeaderChecks",
            "Add-SteamVersionSelectionCompactWorkflowStateChecks",
            "Add-SteamVersionSelectionCompactWorkflowSectionChecks"
        )
}
