. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.audit-modules.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.shared-utils.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.markers.ps1")

function Add-SteamVersionSelectionHelperBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.ps1" `
        "keeps shared helper-boundary audit contracts behind focused modules" `
        @(
            "function Add-SteamVersionSelectionHelperBoundaryChecks",
            "Add-SteamVersionSelectionAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionSharedUtilityBoundaryChecks",
            "Add-SteamVersionSelectionMarkerBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.ps1",
            "audit-steam-version-selection.helper-boundaries.shared-utils.ps1",
            "audit-steam-version-selection.helper-boundaries.markers.ps1"
        )

    Add-SteamVersionSelectionAuditModuleBoundaryChecks

    Add-SteamVersionSelectionSharedUtilityBoundaryChecks

    Add-SteamVersionSelectionMarkerBoundaryChecks
}