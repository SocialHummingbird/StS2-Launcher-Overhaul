. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.download-workflow.ps1")

function Add-SteamVersionSelectionRuntimeAuditModuleBoundaryChecks {
    Add-SteamVersionSelectionRuntimeShellSelectorBoundaryChecks

    Add-SteamVersionSelectionRuntimeBranchNativeBoundaryChecks

    Add-SteamVersionSelectionRuntimeDownloadWorkflowBoundaryChecks
}
