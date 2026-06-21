. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-cloud.safety.push-flow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-cloud.safety.compact-options.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-cloud.safety.cue.ps1")

function Add-SteamVersionSelectionActionCloudSafetyChecks {
    Add-SteamVersionSelectionActionCloudSafetyPushFlowChecks
    Add-SteamVersionSelectionActionCloudSafetyCompactOptionChecks
    Add-SteamVersionSelectionActionCloudSafetyCueChecks
}
