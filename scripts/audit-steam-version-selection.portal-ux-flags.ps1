. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-flags.status.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-flags.workflow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-flags.auth-chrome.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-flags.install-cloud.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-flags.diagnostics.ps1")

function Add-SteamVersionSelectionPortalUxFlagChecks {
    Add-SteamVersionSelectionPortalUxStatusFlagChecks

    Add-SteamVersionSelectionPortalUxWorkflowFlagChecks

    Add-SteamVersionSelectionPortalUxAuthChromeFlagChecks

    Add-SteamVersionSelectionPortalUxInstallCloudFlagChecks

    Add-SteamVersionSelectionPortalUxDiagnosticsFlagChecks
}
