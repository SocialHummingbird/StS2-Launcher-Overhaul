function Add-SteamVersionSelectionRuntimeAuditModuleBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.launcher-shell.ps1" `
        "keeps launcher shell audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLauncherShellChecks",
            "LauncherUI.cs",
            "LauncherUI.Lifecycle.cs",
            "LauncherUI.MainThread.cs",
            "LauncherUI.Viewport.cs",
            "LauncherUI.AutoLaunch.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.ps1" `
        "keeps Steam branch selector audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorChecks",
            "SteamGameBranch.cs",
            "LauncherBranchCatalog.Option.cs",
            "LauncherBranchDropdown.cs",
            "DownloadSection.Branches.cs",
            "ActionSection.Branches.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.ps1" `
        "keeps Steam branch runtime and cache audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeChecks",
            "DepotDownloader.DepotManifestReference.cs",
            "LauncherBranchMarkerFields.cs",
            "LauncherAndroidAppPrivatePath.cs",
            "LauncherGameFiles.Redownload.cs",
            "LauncherGameFiles.CacheCleanup.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.native-routing.ps1" `
        "keeps native selected-branch routing and fallback audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionNativeRoutingChecks",
            "GodotApp.java",
            "SteamBranchInfo.java",
            "LauncherActivity.java",
            "NativeFallbackActivity.java",
            "requires selected branch provenance before consuming native game launch requests"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.download-workflows.ps1" `
        "keeps download, update, and branch-refresh workflow audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionDownloadWorkflowChecks",
            "LauncherModel.Downloads.cs",
            "LauncherModel.Downloads.Action.cs",
            "LauncherController.Downloads.Actions.cs",
            "LauncherController.UpdateChecks.cs",
            "LauncherController.UpdateChecks.ViewUpdate.cs",
            "LauncherController.UpdateChecks.Run.cs",
            "LauncherController.UpdateChecks.Workflow.cs",
            "LauncherController.UpdateChecks.Results.cs"
        )
}
