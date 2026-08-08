function Add-SteamVersionSelectionRuntimeCategoryBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.runtime.ps1" `
        "keeps runtime boundary inventory behind focused selector, runtime, and download modules" `
        @(
            "function Add-SteamVersionSelectionRuntimeAuditModuleBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.download-workflow.ps1",
            "Add-SteamVersionSelectionRuntimeShellSelectorBoundaryChecks",
            "Add-SteamVersionSelectionRuntimeBranchNativeBoundaryChecks",
            "Add-SteamVersionSelectionRuntimeDownloadWorkflowBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.ps1" `
        "keeps branch/native runtime boundary checks delegated to focused child boundary modules" `
        @(
            "function Add-SteamVersionSelectionRuntimeBranchNativeBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.branch-runtime.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.branch-native.native-routing.ps1",
            "Add-SteamVersionSelectionRuntimeBranchBoundaryChecks",
            "Add-SteamVersionSelectionRuntimeNativeRoutingBoundaryChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.ps1" `
        "keeps shell/selector runtime boundary checks delegated to focused child boundary modules" `
        @(
            "function Add-SteamVersionSelectionRuntimeShellSelectorBoundaryChecks",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.shell.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.branch-selector.ps1",
            "audit-steam-version-selection.helper-boundaries.audit-modules.runtime.shell-selector.branch-catalog.ps1",
            "Add-SteamVersionSelectionRuntimeLauncherShellBoundaryChecks",
            "Add-SteamVersionSelectionRuntimeBranchSelectorBoundaryChecks",
            "Add-SteamVersionSelectionRuntimeBranchCatalogBoundaryChecks"
        )
}
