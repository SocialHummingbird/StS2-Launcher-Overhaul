. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.evidence-markers.core.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.evidence-markers.pull.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.evidence-markers.push.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.evidence-markers.blocked-push.ps1")

function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerChecks {
    Add-SteamVersionSelectionCloudSafetyEvidenceMarkerCoreChecks

    Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPullChecks

    Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPushChecks

    Add-SteamVersionSelectionCloudSafetyEvidenceMarkerBlockedPushChecks
}
