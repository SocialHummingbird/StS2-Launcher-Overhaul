. (Join-Path $PSScriptRoot "audit-steam-version-selection.session-auth.model.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.session-auth.connection.ps1")

function Add-SteamVersionSelectionSessionAuthChecks {
    Add-SteamVersionSelectionSessionAuthModelChecks

    Add-SteamVersionSelectionSessionAuthConnectionChecks
}
