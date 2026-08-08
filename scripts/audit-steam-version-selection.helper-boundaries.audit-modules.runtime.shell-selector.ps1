. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.branch-selector.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.branch-catalog.ps1")

function Add-SteamVersionSelectionRuntimeShellSelectorBoundaryChecks {
    Add-SteamVersionSelectionRuntimeLauncherShellBoundaryChecks

    Add-SteamVersionSelectionRuntimeBranchSelectorBoundaryChecks

    Add-SteamVersionSelectionRuntimeBranchCatalogBoundaryChecks
}
