function Add-SteamVersionSelectionPortalActionCodeSectionBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.code-section.ps1" `
        "keeps compact Steam Guard code-section audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCodeSectionChecks",
            "audit-steam-version-selection.code-section.construction.ps1",
            "audit-steam-version-selection.code-section.controls.ps1",
            "audit-steam-version-selection.code-section.responsive.ps1",
            "audit-steam-version-selection.code-section.submission.ps1",
            "Add-SteamVersionSelectionCodeSectionConstructionChecks",
            "Add-SteamVersionSelectionCodeSectionControlChecks",
            "Add-SteamVersionSelectionCodeSectionResponsiveChecks",
            "Add-SteamVersionSelectionCodeSectionSubmissionChecks"
        )
}
