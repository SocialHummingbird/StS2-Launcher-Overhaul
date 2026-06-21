param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "static-audit-utils.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.helper-boundaries.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.launcher-shell.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-selector.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-runtime.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.native-routing.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-availability.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.download-workflows.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.session-auth.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.automation.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.local-login.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.confirmations.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.cloud-safety.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-panel.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-labels.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.section-setup.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.safe-flow-guide.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-drawer.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.diagnostics-reporting.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.evidence-tooling.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.branch-evidence-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-validation-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.login-portal-evidence-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-status-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.release-docs.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.beta-integrity.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.governance.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-chrome.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.status-capsule.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-behavior.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-workflow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.code-section.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-section-flow.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.compact-install.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-warmup.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.startup-recovery.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.action-section.ps1")
. (Join-Path $PSScriptRoot "audit-steam-version-selection.portal-ux-support.ps1")
Initialize-StaticAudit -ScriptRoot $PSScriptRoot -Quiet:$Quiet

Add-SteamVersionSelectionHelperBoundaryChecks

Add-SteamVersionSelectionLauncherShellChecks

Add-SteamVersionSelectionBranchSelectorChecks

Add-SteamVersionSelectionBranchRuntimeChecks

Add-SteamVersionSelectionNativeRoutingChecks

Add-SteamVersionSelectionBranchAvailabilityChecks

Add-SteamVersionSelectionDownloadWorkflowChecks

Add-SteamVersionSelectionSessionAuthChecks

Add-SteamVersionSelectionAutomationChecks

Add-SteamVersionSelectionLocalLoginChecks

Add-SteamVersionSelectionConfirmationChecks

Add-SteamVersionSelectionCloudSafetyChecks

Add-SteamVersionSelectionLoginPanelChecks

Add-SteamVersionSelectionCompactLabelChecks

Add-SteamVersionSelectionSectionSetupChecks

Add-SteamVersionSelectionStatusCapsuleChecks

Add-SteamVersionSelectionSafeFlowGuideChecks

Add-SteamVersionSelectionDiagnosticsDrawerChecks

Add-SteamVersionSelectionDiagnosticsReportingChecks

Add-SteamVersionSelectionEvidenceToolingChecks

Add-SteamVersionSelectionReleaseDocsChecks

Add-SteamVersionSelectionBetaIntegrityChecks

Add-SteamVersionSelectionPortalChromeChecks

Add-SteamVersionSelectionCompactWorkflowChecks

Add-SteamVersionSelectionCodeSectionChecks


Add-SteamVersionSelectionCompactSectionFlowChecks

Add-SteamVersionSelectionCompactInstallChecks


Add-SteamVersionSelectionStartupWarmupChecks

Add-SteamVersionSelectionStartupRecoveryChecks


Add-SteamVersionSelectionActionSectionChecks

Add-SteamVersionSelectionPortalUxSupportChecks

Add-SteamVersionSelectionPortalBehaviorChecks

Add-SteamVersionSelectionGovernanceChecks

Add-SteamVersionSelectionBranchEvidenceDocsChecks

Add-SteamVersionSelectionLoginValidationDocsChecks

Add-SteamVersionSelectionLoginPortalEvidenceDocsChecks

Add-SteamVersionSelectionReleaseStatusDocsChecks

Complete-StaticAudit `
    -FailureHeading "Steam version-selection static audit failed:" `
    -SuccessMessage "Steam version-selection static audit passed: {0} checks."
