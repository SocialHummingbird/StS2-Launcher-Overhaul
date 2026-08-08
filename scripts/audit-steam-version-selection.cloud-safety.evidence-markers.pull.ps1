function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPullChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Pull.cs" `
        "records manual Pull outcome and branch history without making Pull a Push prerequisite" `
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
            "LastManualPullOutcome",
            "LastManualPullOutcomeDetail",
            "ManualPullOutcomePrefix",
            "ManualPullOutcomeDetailPrefix",
            "LastManualPullIsAfterBranchSwitch",
            "LastManualPullMatchesSelectedBranch",
            "WriteManualPullMarker",
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Pull.cs" `
        "keeps Pull evidence free of obsolete Push-prerequisite fields" `
        @(
            "BaselineManualPushPrerequisitesSatisfied",
            "LastManualPullBeforePushCompletionRecorded",
            "ManualPullCompletedBeforePushPrefix",
            "ManualPullCompletedBeforeBranchSwitchPushPrefix"
        )
}
