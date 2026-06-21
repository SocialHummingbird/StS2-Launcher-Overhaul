. (Join-Path $PSScriptRoot "audit-steam-version-selection.status-capsule.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.status-capsule.compact.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.status-capsule.detail.ps1")

function Add-SteamVersionSelectionStatusCapsuleChecks {
    Add-SteamVersionSelectionStatusCapsuleShellChecks

    Add-SteamVersionSelectionStatusCapsuleCompactChecks

    Add-SteamVersionSelectionStatusCapsuleDetailChecks
}
