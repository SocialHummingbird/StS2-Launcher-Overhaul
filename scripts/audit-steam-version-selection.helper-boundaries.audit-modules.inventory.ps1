function Add-SteamVersionSelectionAuditModuleInventoryBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.ps1" `
        "keeps audit-module boundary orchestration delegated to focused inventory, category, and implementation modules" `
        @(
            "function Add-SteamVersionSelectionAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionAuditModuleInventoryBoundaryChecks",
            "Add-SteamVersionSelectionAuditModuleCategoryBoundaryChecks",
            "Add-SteamVersionSelectionAuditModuleSubcategoryBoundaryChecks",
            "Add-SteamVersionSelectionRuntimeAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionAuthCloudAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsAuditModuleBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionAuditModuleBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.inventory.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.categories.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.categories.ps1" `
        "keeps audit-module category boundary inventory behind focused runtime, auth/cloud, portal/action, and support/docs modules" `
        @(
            "function Add-SteamVersionSelectionAuditModuleCategoryBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.categories.runtime.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.categories.auth-cloud.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.categories.portal-action.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.categories.support-docs.ps1",
            "Add-SteamVersionSelectionRuntimeCategoryBoundaryChecks",
            "Add-SteamVersionSelectionAuthCloudCategoryBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionCategoryBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsCategoryBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.ps1" `
        "keeps audit-module subcategory boundary inventory behind focused portal/action and support/docs modules" `
        @(
            "function Add-SteamVersionSelectionAuditModuleSubcategoryBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.portal-action.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.subcategories.support-docs.ps1",
            "Add-SteamVersionSelectionPortalActionSubcategoryBoundaryChecks",
            "Add-SteamVersionSelectionSupportDocsSubcategoryBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.inventory.ps1" `
        "keeps the audit-module category file inventory out of the execution orchestrator" `
        @(
            "function Add-SteamVersionSelectionAuditModuleInventoryBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.download-workflow.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.session.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.cloud-safety.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.auth-cloud.login-panel.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-docs.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.evidence-docs.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.support-docs.login-portal.validation-matrix.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.compact-install.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.visibility.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.branch-availability.ps1"
        )
}
