. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.ps1")

function Add-SteamVersionSelectionAuditModuleBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.ps1" `
        "keeps audit-module boundary inventory behind focused category modules" `
        @(
            "function Add-SteamVersionSelectionAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionRuntimeAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionAuthCloudAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionAuditModuleBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.ps1"
        )

    Add-SteamVersionSelectionRuntimeAuditModuleBoundaryChecks

    Add-SteamVersionSelectionAuthCloudAuditModuleBoundaryChecks

    Add-SteamVersionSelectionSupportDocsAuditModuleBoundaryChecks

    Add-SteamVersionSelectionPortalActionAuditModuleBoundaryChecks
}
