. (Join-Path $PSScriptRoot "audit-steam-version-selection.registration.runtime.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.registration.auth-cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.registration.support-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.registration.portal-action.ps1")

function Add-SteamVersionSelectionStaticAuditChecks {
    Add-SteamVersionSelectionRuntimeAuditChecks

    Add-SteamVersionSelectionAuthCloudAuditChecks

    Add-SteamVersionSelectionCompactSupportAuditChecks

    Add-SteamVersionSelectionPortalActionAuditChecks
}
