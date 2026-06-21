. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.markers.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.backups.ps1")

function Add-SteamVersionSelectionAuthCloudSafetyBoundaryChecks {
    Add-SteamVersionSelectionAuthCloudSafetyShellBoundaryChecks
    Add-SteamVersionSelectionAuthCloudSafetyMarkerBoundaryChecks
    Add-SteamVersionSelectionAuthCloudSafetyBackupBoundaryChecks
}
