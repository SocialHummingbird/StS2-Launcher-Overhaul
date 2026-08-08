function Add-SteamVersionSelectionPortalActionCategoryBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.ps1" `
        "keeps portal/action boundary inventory behind focused UI and action boundary modules" `
        @(
            "function Add-SteamVersionSelectionPortalActionAuditModuleBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.ps1",
            "Add-SteamVersionSelectionPortalActionChromeWorkflowBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionStartupBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionReadyActionBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionPortalUxBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.ps1" `
        "keeps startup portal/action boundary checks delegated to focused child boundary modules" `
        @(
            "function Add-SteamVersionSelectionPortalActionStartupBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.shell.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.shader.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.status.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.startup.recovery.ps1",
            "Add-SteamVersionSelectionPortalActionStartupShellBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionStartupShaderBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionStartupStatusBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionStartupRecoveryBoundaryChecks"
        )
}
