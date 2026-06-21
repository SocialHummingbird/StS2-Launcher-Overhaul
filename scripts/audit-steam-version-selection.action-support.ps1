. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-support.foundation.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-support.construction.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-support.labels.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-support.diagnostics.ps1")

function Add-SteamVersionSelectionActionSupportChecks {
    Add-SteamVersionSelectionActionSupportFoundationChecks

    Add-SteamVersionSelectionActionSupportConstructionChecks

    Add-SteamVersionSelectionActionSupportLabelChecks
}
