. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.version.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.version.summary.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.version.actions.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.version.layout.ps1")

function Add-SteamVersionSelectionCompactInstallVersionChecks {
    Add-SteamVersionSelectionCompactInstallVersionShellChecks

    Add-SteamVersionSelectionCompactInstallVersionSummaryChecks

    Add-SteamVersionSelectionCompactInstallVersionActionLabelChecks

    Add-SteamVersionSelectionCompactInstallVersionLayoutChecks
}
