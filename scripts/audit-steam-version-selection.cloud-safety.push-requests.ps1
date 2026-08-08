. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.push-requests.gates.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.push-requests.request.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.push-requests.execution.ps1")

function Add-SteamVersionSelectionCloudSafetyPushRequestChecks {
    Add-SteamVersionSelectionCloudSafetyPushGateChecks

    Add-SteamVersionSelectionCloudSafetyPushRequestConstructionChecks

    Add-SteamVersionSelectionCloudSafetyPushExecutionChecks
}
