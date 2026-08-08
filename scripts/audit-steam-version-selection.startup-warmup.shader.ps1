. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.shader.lifecycle.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.shader.execution.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.shader.ui.ps1")

function Add-SteamVersionSelectionStartupWarmupShaderChecks {
    Add-SteamVersionSelectionStartupWarmupShaderLifecycleChecks

    Add-SteamVersionSelectionStartupWarmupShaderExecutionChecks

    Add-SteamVersionSelectionStartupWarmupShaderUiChecks
}
