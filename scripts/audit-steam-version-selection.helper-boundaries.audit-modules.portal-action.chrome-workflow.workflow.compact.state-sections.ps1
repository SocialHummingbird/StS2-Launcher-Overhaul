function Add-SteamVersionSelectionPortalActionCompactWorkflowStateSectionBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.state.ps1" `
        "keeps compact workflow state and navigation audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowStateChecks",
            "LauncherView.CompactWorkflow.Data.cs",
            "LauncherView.CompactWorkflow.State.cs",
            "LauncherView.CompactWorkflow.Navigation.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.sections.ps1" `
        "keeps compact workflow section-transition audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowSectionChecks",
            "LauncherView.Sections.Auth.cs",
            "LauncherView.Sections.Download.cs",
            "LauncherView.Sections.Actions.cs"
        )
}
