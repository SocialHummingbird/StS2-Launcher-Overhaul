function Add-SteamVersionSelectionSupportDocsLoginPortalEvidenceBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.ps1" `
        "keeps login portal evidence-template audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsChecks",
            "audit-steam-version-selection.login-portal-evidence-docs.auth-status.ps1",
            "audit-steam-version-selection.login-portal-evidence-docs.compact-workflow.ps1",
            "audit-steam-version-selection.login-portal-evidence-docs.install-cloud.ps1",
            "audit-steam-version-selection.login-portal-evidence-docs.validation-matrix.ps1",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsAuthStatusChecks",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsCompactWorkflowChecks",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsInstallCloudChecks",
            "Add-SteamVersionSelectionLoginPortalEvidenceDocsValidationMatrixChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.auth-status.ps1" `
        "keeps secret-safe login and compact status evidence-template checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsAuthStatusChecks",
            "Steam username/email redacted",
            "Native integrated Steam login panel opens automatically",
            "Compact launcher sign-in shows Sign in with Steam before helper copy",
            "Launcher compact plain-language status copy supported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.compact-workflow.ps1" `
        "keeps compact launcher workflow, layout, and status evidence-template checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsCompactWorkflowChecks",
            "Quick-start guide visible",
            "Home, Saves, Versions, Mods, and Help are all reachable through persistent navigation",
            "Changing destinations resets only the destination page to its top",
            "Rotating or folding the device preserves the current process"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-portal-evidence-docs.install-cloud.ps1" `
        "keeps compact install, play/sync, cloud-safety, and recovery evidence-template checks focused" `
        @(
            "function Add-SteamVersionSelectionLoginPortalEvidenceDocsInstallCloudChecks",
            "Compact Game Install selected-version summary is a readable touch-safe card",
            "Compact Pull action says Get Steam Saves / Download to Android",
            "Compact armed Push warning says Steam Cloud overwrite",
            "Compact Home keeps Start Game and Safe Start above the fold"
        )
}
