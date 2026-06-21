function Add-SteamVersionSelectionSupportDocsCompactSectionSetupBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.section-setup.ps1" `
        "keeps compact section setup audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionSectionSetupChecks",
            "audit-steam-version-selection.section-setup.shell.ps1",
            "audit-steam-version-selection.section-setup.headers.ps1",
            "audit-steam-version-selection.section-setup.cues.ps1",
            "Add-SteamVersionSelectionSectionSetupShellChecks",
            "Add-SteamVersionSelectionSectionSetupHeaderChecks",
            "Add-SteamVersionSelectionSectionSetupCueChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.section-setup.shell.ps1" `
        "keeps compact section setup shell audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionSectionSetupShellChecks",
            "LauncherSectionSetup.cs",
            "ConfigureHiddenSection",
            "BuildSectionHeader"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.section-setup.headers.ps1" `
        "keeps compact section header audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionSectionSetupHeaderChecks",
            "LauncherSectionSetup.Header.cs",
            "LauncherSectionSetup.Header.Compact.cs",
            "LauncherSectionSetup.Header.Style.cs",
            "BuildCompactSectionHeader",
            "BuildHeaderStyle"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.section-setup.cues.ps1" `
        "keeps compact section cue-text audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionSectionSetupCueChecks",
            "LoginSection.cs",
            "CodeSection.cs",
            "DownloadSection.cs",
            "ActionSection.Construction.cs"
        )
}
