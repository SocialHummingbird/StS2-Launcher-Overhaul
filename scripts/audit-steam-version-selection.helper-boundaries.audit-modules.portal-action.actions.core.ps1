function Add-SteamVersionSelectionPortalActionReadyActionCoreBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.action-section.ps1" `
        "keeps ready-state action audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionActionSectionChecks",
            "Add-SteamVersionSelectionActionCoreChecks",
            "Add-SteamVersionSelectionActionSupportChecks",
            "Add-SteamVersionSelectionActionCloudControlsChecks",
            "Add-SteamVersionSelectionActionVisibilityChecks",
            "Add-SteamVersionSelectionActionCloudSafetyChecks",
            "Add-SteamVersionSelectionActionSupportDiagnosticsChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-core.ps1" `
        "keeps ready-state action core audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionActionCoreChecks",
            "audit-steam-version-selection.action-core.construction.ps1",
            "audit-steam-version-selection.action-core.button-styles.ps1",
            "audit-steam-version-selection.action-core.ready-summary.ps1",
            "audit-steam-version-selection.action-core.layout.ps1",
            "Add-SteamVersionSelectionActionCoreConstructionChecks",
            "Add-SteamVersionSelectionActionCoreButtonStyleChecks",
            "Add-SteamVersionSelectionActionCoreReadySummaryChecks",
            "Add-SteamVersionSelectionActionCoreLayoutChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-core.construction.ps1" `
        "keeps ready-state branch-control construction audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCoreConstructionChecks",
            "ActionSection.Construction.cs",
            "ActionSection.Construction.Branch.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-core.button-styles.ps1" `
        "keeps ready-state button-style audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCoreButtonStyleChecks",
            "LauncherButtonStyles.cs",
            "LauncherButtonStyles.Dropdown.cs",
            "LauncherButtonStyles.State.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-core.ready-summary.ps1" `
        "keeps ready-state summary and save-check audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCoreReadySummaryChecks",
            "ActionSection.Construction.Ready.cs",
            "ActionSection.ReadySummary.cs",
            "ActionSection.ReadySummary.Style.cs",
            "ActionSection.CloudSafety.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-core.layout.ps1" `
        "keeps ready-state layout and drawer-copy audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCoreLayoutChecks",
            "ActionSection.Layout.cs",
            "ActionSection.Branches.Text.cs",
            "ActionSection.CloudSafety.cs",
            "ActionSection.CloudOptions.cs"
        )
}
