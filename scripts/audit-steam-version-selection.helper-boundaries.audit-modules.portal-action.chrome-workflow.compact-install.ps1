function Add-SteamVersionSelectionPortalActionCompactInstallBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.ps1" `
        "keeps compact install/version/download audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCompactInstallChecks",
            "audit-steam-version-selection.compact-install.version.ps1",
            "audit-steam-version-selection.compact-install.download.ps1",
            "audit-steam-version-selection.compact-install.metrics.ps1",
            "Add-SteamVersionSelectionCompactInstallVersionChecks",
            "Add-SteamVersionSelectionCompactInstallDownloadChecks",
            "Add-SteamVersionSelectionCompactInstallMetricChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.version.ps1" `
        "keeps compact install version-selection layout audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCompactInstallVersionChecks",
            "audit-steam-version-selection.compact-install.version.shell.ps1",
            "audit-steam-version-selection.compact-install.version.summary.ps1",
            "audit-steam-version-selection.compact-install.version.actions.ps1",
            "audit-steam-version-selection.compact-install.version.layout.ps1",
            "Add-SteamVersionSelectionCompactInstallVersionShellChecks",
            "Add-SteamVersionSelectionCompactInstallVersionSummaryChecks",
            "Add-SteamVersionSelectionCompactInstallVersionActionLabelChecks",
            "Add-SteamVersionSelectionCompactInstallVersionLayoutChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.version.shell.ps1" `
        "keeps compact install version shell and drawer audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactInstallVersionShellChecks",
            "DownloadSection.cs",
            "DownloadSection.Construction.Version.cs",
            "DownloadSection.Construction.Version.Selected.cs",
            "DownloadSection.Construction.Version.Dropdown.cs",
            "DownloadSection.Construction.Version.Refresh.cs",
            "DownloadSection.Branches.Text.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.version.summary.ps1" `
        "keeps compact install selected-version summary audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactInstallVersionSummaryChecks",
            "DownloadSection.CompactVersion.cs",
            "DownloadSection.CompactVersion.Summary.cs",
            "DownloadSection.CompactVersion.Summary.Style.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.version.actions.ps1" `
        "keeps compact install version action-label audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactInstallVersionActionLabelChecks",
            "DownloadSection.CompactVersion.ActionButton.cs",
            "DownloadSection.CompactDownload.Text.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.version.layout.ps1" `
        "keeps compact install responsive ordering audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactInstallVersionLayoutChecks",
            "LauncherView.Layout.PrimaryColumn.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.download.ps1" `
        "keeps compact install primary download and progress audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactInstallDownloadChecks",
            "DownloadSection.CompactDownload.ActionButton.cs",
            "DownloadSection.CompactDownload.Text.cs",
            "DownloadSection.CompactDownload.cs",
            "DownloadSection.Progress.cs",
            "DownloadSection.Construction.Download.cs",
            "CompactDownloadProgressText"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.metrics.ps1" `
        "keeps compact install progress and shared scaling metric audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCompactInstallMetricChecks",
            "LauncherComponentTheme.cs",
            "LauncherViewLayoutMetrics.cs",
            "CompactProgressBarHeight",
            "MidpointRounding"
        )
}
