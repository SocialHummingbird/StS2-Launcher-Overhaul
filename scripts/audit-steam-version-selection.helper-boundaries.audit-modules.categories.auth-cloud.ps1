function Add-SteamVersionSelectionAuthCloudCategoryBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.ps1" `
        "keeps auth/cloud boundary inventory behind focused session, cloud-safety, and login-panel modules" `
        @(
            "function Add-SteamVersionSelectionAuthCloudAuditModuleBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.session.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.login-panel.ps1",
            "Add-SteamVersionSelectionAuthCloudSessionBoundaryChecks",
            "Add-SteamVersionSelectionAuthCloudSafetyBoundaryChecks",
            "Add-SteamVersionSelectionAuthCloudLoginPanelBoundaryChecks"
        )
}
