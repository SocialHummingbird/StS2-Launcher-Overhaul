. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.status.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.status.android.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.status.cleanup.ps1")

function Add-SteamVersionSelectionStartupWarmupStatusChecks {
    Add-SteamVersionSelectionStartupWarmupStatusShellChecks

    Add-SteamVersionSelectionStartupWarmupStatusAndroidChecks

    Add-SteamVersionSelectionStartupWarmupStatusCleanupChecks
}
