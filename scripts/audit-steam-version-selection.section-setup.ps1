. (Join-Path $PSScriptRoot "audit-steam-version-selection.section-setup.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.section-setup.headers.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.section-setup.cues.ps1")

function Add-SteamVersionSelectionSectionSetupChecks {
    Add-SteamVersionSelectionSectionSetupShellChecks

    Add-SteamVersionSelectionSectionSetupHeaderChecks

    Add-SteamVersionSelectionSectionSetupCueChecks
}
