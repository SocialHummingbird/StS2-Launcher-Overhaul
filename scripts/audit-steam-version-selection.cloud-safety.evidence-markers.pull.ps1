function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPullChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Pull.cs" `
        "records successful manual Pull evidence for branch-switch Push safety" `
        @(
            "HasManualPullAfterBranchSwitch",
            "LastManualPullUtc",
            "LastManualPullUtcParseable",
            "LastManualPullSelectedBranch",
            "LastManualPullSelectedBranchSelectionKind",
            "LastManualPullSelectorMode",
            "LastManualPullSelectedVersion",
            "LastManualPullSelectedVersionSlotKind",
            "LastManualPullSelectedVersionSlotDirectory",
            "LastManualPullCompletionRecorded",
            "LastManualPullBeforePushCompletionRecorded",
            "BaselineManualPushPrerequisitesSatisfied",
            "ManualPullCompletedBeforePushPrefix",
            "LastManualPullIsAfterBranchSwitch",
            "LastManualPullMatchesSelectedBranch",
            "WriteManualPullMarker",
            "ManualPullCompletedBeforeBranchSwitchPushPrefix",
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )
}
