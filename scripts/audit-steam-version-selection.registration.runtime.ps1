function Add-SteamVersionSelectionRuntimeAuditChecks {
    Add-SteamVersionSelectionHelperBoundaryChecks

    Add-SteamVersionSelectionLauncherShellChecks

    Add-SteamVersionSelectionBranchSelectorChecks

    Add-SteamVersionSelectionBranchRuntimeChecks

    Add-SteamVersionSelectionNativeRoutingChecks

    Add-SteamVersionSelectionBranchAvailabilityChecks

    Add-SteamVersionSelectionDownloadWorkflowChecks
}
