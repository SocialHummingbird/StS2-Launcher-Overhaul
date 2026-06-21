. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.session.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.login-panel.ps1")

function Add-SteamVersionSelectionAuthCloudAuditModuleBoundaryChecks {
    Add-SteamVersionSelectionAuthCloudSessionBoundaryChecks

    Add-SteamVersionSelectionAuthCloudSafetyBoundaryChecks

    Add-SteamVersionSelectionAuthCloudLoginPanelBoundaryChecks
}
