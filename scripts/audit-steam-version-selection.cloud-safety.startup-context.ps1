function Add-SteamVersionSelectionCloudSafetyStartupContextChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchSwitchCoordinator.cs" `
        "uses centralized selector guidance in branch-switch confirmation" `
        @(
            "BranchSwitchConfirmationMessage",
            "SteamGameBranch\.SelectorInstallSlotHelpText",
            "LauncherBranchCatalog\.ReadVisibleBranches",
            "SelectedOptionStatus",
            "SelectedOptionDownloadProblem",
            "AppendLog\(STS2Mobile\.Steam\.SteamGameBranch\.SelectorInstallSlotHelpText",
            "Steam Cloud Push will require backup storage permission"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.cs" `
        "uses selected-version runtime evidence in ready and download-required status copy" `
        @(
            "SelectedVersionReadyStatus",
            "SelectedVersionDownloadRequiredStatus",
            "SteamGameInstallPaths\.VersionSlotKind",
            "Active install slot",
            "RefreshSelectedRuntimeSlotEvidence",
            "LauncherRuntimeSlotEvidence\.Write"
        )

}
