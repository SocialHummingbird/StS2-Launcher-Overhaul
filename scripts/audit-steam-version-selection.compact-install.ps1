. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.version.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.download.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.metrics.ps1")

function Add-SteamVersionSelectionCompactInstallChecks {
    Add-SteamVersionSelectionCompactInstallVersionChecks

    Add-SteamVersionSelectionCompactInstallDownloadChecks

    Add-SteamVersionSelectionCompactInstallMetricChecks
}
