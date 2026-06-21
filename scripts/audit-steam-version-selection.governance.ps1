function Add-SteamVersionSelectionGovernanceChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.ps1" `
        "keeps the static audit entrypoint limited to initialization, registration, and completion" `
        @(
            "static-audit-utils.ps1",
            "audit-steam-version-selection.modules.ps1",
            "audit-steam-version-selection.registration.ps1",
            "Initialize-StaticAudit",
            "Add-SteamVersionSelectionStaticAuditChecks",
            "Complete-StaticAudit"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.modules.ps1" `
        "keeps static audit module loading isolated from the entrypoint" `
        @(
            "audit-steam-version-selection.helper-boundaries.ps1",
            "audit-steam-version-selection.branch-selector.ps1",
            "audit-steam-version-selection.cloud-safety.ps1",
            "audit-steam-version-selection.release-docs.ps1",
            "audit-steam-version-selection.action-section.ps1",
            "audit-steam-version-selection.portal-ux-support.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.registration.ps1" `
        "keeps static audit check registration delegated to focused domain groups" `
        @(
            "function Add-SteamVersionSelectionStaticAuditChecks",
            "audit-steam-version-selection.registration.runtime.ps1",
            "audit-steam-version-selection.registration.auth-cloud.ps1",
            "audit-steam-version-selection.registration.support-docs.ps1",
            "audit-steam-version-selection.registration.portal-action.ps1",
            "Add-SteamVersionSelectionRuntimeAuditChecks",
            "Add-SteamVersionSelectionAuthCloudAuditChecks",
            "Add-SteamVersionSelectionCompactSupportAuditChecks",
            "Add-SteamVersionSelectionPortalActionAuditChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.registration.runtime.ps1" `
        "keeps runtime and download audit registration focused" `
        @(
            "function Add-SteamVersionSelectionRuntimeAuditChecks",
            "Add-SteamVersionSelectionHelperBoundaryChecks",
            "Add-SteamVersionSelectionBranchSelectorChecks",
            "Add-SteamVersionSelectionBranchRuntimeChecks",
            "Add-SteamVersionSelectionDownloadWorkflowChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.registration.auth-cloud.ps1" `
        "keeps authentication and cloud audit registration focused" `
        @(
            "function Add-SteamVersionSelectionAuthCloudAuditChecks",
            "Add-SteamVersionSelectionSessionAuthChecks",
            "Add-SteamVersionSelectionCloudSafetyChecks",
            "Add-SteamVersionSelectionLoginPanelChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.registration.support-docs.ps1" `
        "keeps compact support and documentation audit registration focused" `
        @(
            "function Add-SteamVersionSelectionCompactSupportAuditChecks",
            "Add-SteamVersionSelectionCompactLabelChecks",
            "Add-SteamVersionSelectionDiagnosticsReportingChecks",
            "Add-SteamVersionSelectionReleaseDocsChecks",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.registration.portal-action.ps1" `
        "keeps portal and action audit registration focused" `
        @(
            "function Add-SteamVersionSelectionPortalActionAuditChecks",
            "Add-SteamVersionSelectionPortalChromeChecks",
            "Add-SteamVersionSelectionCompactInstallChecks",
            "Add-SteamVersionSelectionStartupWarmupChecks",
            "Add-SteamVersionSelectionActionSectionChecks",
            "Add-SteamVersionSelectionPortalBehaviorChecks"
        )

    Add-Check `
        ".github\workflows\steam-version-selection-static-audit.yml" `
        "runs the static audit in CI" `
        @(
            "Steam Version Selection Static Audit",
            "pull_request",
            "workflow_dispatch",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1"
        )

    Add-Check `
        ".github\workflows\overhaul-governance-ci.yml" `
        "requires Steam version-selection guardrail scaffolding" `
        @(
            "steam-version-selection-static-audit\.yml",
            "audit-steam-version-selection\.ps1",
            "audit-steam-branch-guidance-parity\.ps1",
            "steam-version-selection-validation\.md"
        )

    Add-Check `
        ".github\PULL_REQUEST_TEMPLATE.md" `
        "prompts reviewers to call out Steam version-selection risk" `
        @(
            "Steam version-selection static audit run",
            "Steam branch guidance parity audit run",
            "Steam version-selection risk",
            "steam_branch\.txt",
            "Pull-after-switch"
        )

    Add-Check `
        ".github\pull_request_template\pull_request_template.md" `
        "prompts reviewers to call out Steam version-selection risk" `
        @(
            "Steam version-selection static audit run",
            "Steam branch guidance parity audit run",
            "Steam version-selection risk",
            "steam_branch\.txt",
            "Pull-after-switch"
        )
}
