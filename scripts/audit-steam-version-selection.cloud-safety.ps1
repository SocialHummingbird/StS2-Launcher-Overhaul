. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.branch-switch.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.push-requests.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.evidence-markers.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.local-backups.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.startup-context.ps1")

function Add-SteamVersionSelectionCloudSafetyChecks {
    Add-SteamVersionSelectionCloudSafetyBranchSwitchChecks

    Add-SteamVersionSelectionCloudSafetyPushRequestChecks

    Add-SteamVersionSelectionCloudSafetyEvidenceMarkerChecks

    Add-SteamVersionSelectionCloudSafetyLocalBackupChecks

    Add-SteamVersionSelectionCloudSafetyStartupContextChecks
}
