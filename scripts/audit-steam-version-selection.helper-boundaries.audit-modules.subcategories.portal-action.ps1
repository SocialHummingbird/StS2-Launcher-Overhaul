function Add-SteamVersionSelectionPortalActionSubcategoryBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.ps1" `
        "keeps chrome/workflow boundary inventory behind focused chrome, workflow, and install modules" `
        @(
            "function Add-SteamVersionSelectionPortalActionChromeWorkflowBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.chrome-status.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.workflow.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.chrome-workflow.compact-install.ps1",
            "Add-SteamVersionSelectionPortalActionChromeStatusBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionWorkflowBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionCompactInstallBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.ps1" `
        "keeps portal UX and branch availability boundary inventory behind focused modules" `
        @(
            "function Add-SteamVersionSelectionPortalActionPortalUxBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.support.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.flags.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.features.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.portal-ux.branch-availability.ps1",
            "Add-SteamVersionSelectionPortalActionPortalUxSupportBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionPortalUxFlagBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionPortalUxFeatureBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionBranchAvailabilityBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.ps1" `
        "keeps ready-state action boundary inventory behind focused core, support, cloud, and visibility modules" `
        @(
            "function Add-SteamVersionSelectionPortalActionReadyActionBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.core.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.support.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.visibility.ps1",
            "Add-SteamVersionSelectionPortalActionReadyActionCoreBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionReadyActionSupportBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionReadyActionCloudBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionReadyActionVisibilityBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.ps1" `
        "keeps ready-state cloud boundary inventory behind focused controls and safety modules" `
        @(
            "function Add-SteamVersionSelectionPortalActionReadyActionCloudBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.controls.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.portal-action.actions.cloud.safety.ps1",
            "Add-SteamVersionSelectionPortalActionReadyActionCloudControlBoundaryChecks",
            "Add-SteamVersionSelectionPortalActionReadyActionCloudSafetyBoundaryChecks"
        )
}
