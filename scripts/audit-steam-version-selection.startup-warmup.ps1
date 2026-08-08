. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.startup-mode.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.shader.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.status.ps1")

function Add-SteamVersionSelectionStartupWarmupChecks {
    Add-SteamVersionSelectionStartupWarmupStartupModeChecks

    Add-SteamVersionSelectionStartupWarmupShaderChecks

    Add-SteamVersionSelectionStartupWarmupStatusChecks
}
