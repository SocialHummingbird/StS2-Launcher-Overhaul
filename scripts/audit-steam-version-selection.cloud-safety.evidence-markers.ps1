function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.cs" `
        "declares manual cloud evidence marker filenames and paths" `
        @(
            "last_manual_cloud_pull\.txt",
            "LastManualPushMarkerFileName",
            "last_manual_cloud_push_blocked\.txt"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Fields.cs" `
        "centralizes manual cloud evidence marker prefixes" `
        @(
            "SelectedBranchPrefix = ""Selected branch:""",
            "SelectedBranchSelectionKindPrefix = ""Selected branch selection kind:""",
            "SelectorModePrefix = ""Steam branch selector mode:""",
            "SelectedVersionPrefix = ""Selected version:""",
            "SelectedVersionSlotKindPrefix = ""Selected version slot kind:""",
            "SelectedVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
            "SelectedBranchNotePrefix = ""Selected branch note:""",
            "ManualPullCompletedBeforePushPrefix = ""Manual Pull completed before Push:""",
            "ManualPullCompletedBeforeBranchSwitchPushPrefix = ""Manual Pull completed before branch-switch Push:""",
            "PrePushLocalBackupEvidenceCountPrefix = ""Pre-Push local backup evidence count:""",
            "PrePushCloudBackupEvidenceCountPrefix = ""Pre-Push cloud backup evidence count:""",
            "LatestPrePushLocalBackupUtcPrefix = ""Latest pre-Push local backup UTC:""",
            "LatestPrePushCloudBackupUtcPrefix = ""Latest pre-Push cloud backup UTC:""",
            "ImportantLocalSaveEvidenceCountPrefix = ""Important Android local save evidence count:""",
            "BaselineManualPushPrerequisitesSatisfiedPrefix = ""Baseline manual Push prerequisites satisfied:""",
            "BranchSwitchPrePushBackupEvidenceSatisfiedPrefix = ""Branch-switch pre-Push backup evidence satisfied:""",
            "BranchSwitchManualPushPrerequisitesSatisfiedPrefix = ""Branch-switch manual Push prerequisites satisfied:""",
            "ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix = ""Manual Push completed after branch-switch safety gates:""",
            "BlockedReasonPrefix = ""Blocked reason:""",
            "ManualPushBlockedBeforeUploadPrefix = ""Manual Push blocked before upload:""",
            "ManualPushBlockedReasonPrefix = ""Manual Push blocked:"""
        )

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

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.cs" `
        "reads successful manual Push timestamps from the Push marker" `
        @(
            "LastManualPushUtc",
            "LastManualPushUtcParseable",
            "LastManualPushMarkerPath",
            "CultureInfo\.InvariantCulture"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Latest.cs" `
        "selects latest manual Push evidence between completed and blocked outcomes" `
        @(
            "LatestManualPushEvidenceOutcome",
            "LatestManualPushEvidenceUtc",
            "LatestManualPushEvidenceSelectedBranch",
            "LatestManualPushEvidenceSelectedBranchSelectionKind",
            "LatestManualPushEvidenceSelectorMode",
            "LatestManualPushEvidenceSelectedVersion",
            "LatestManualPushEvidenceSelectedVersionSlotKind",
            "LatestManualPushEvidenceSelectedVersionSlotDirectory",
            "LatestManualPushEvidenceReason",
            "blocked-before-upload",
            "Manual Push completed",
            "LatestManualPushEvidenceBlocked"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Read.cs" `
        "reads completed manual Push marker metadata and backup evidence" `
        @(
            "LastManualPushSelectedBranch",
            "LastManualPushSelectedBranchSelectionKind",
            "LastManualPushSelectorMode",
            "LastManualPushSelectedVersion",
            "LastManualPushSelectedVersionSlotKind",
            "LastManualPushSelectedVersionSlotDirectory",
            "LastManualPushRecordedLocalBackupCount",
            "LastManualPushRecordedCloudBackupCount",
            "LastManualPushRecordedLatestLocalBackupUtc",
            "LastManualPushRecordedLatestCloudBackupUtc",
            "LastManualPushRecordedImportantLocalSaveEvidenceCount",
            "LastManualPushRecordedBaselinePrerequisitesSatisfied",
            "LastManualPushCompletionRecorded",
            "LastManualPushPrePushBackupEvidenceSatisfied",
            "PrePushLocalBackupEvidenceCountPrefix",
            "PrePushCloudBackupEvidenceCountPrefix",
            "LatestPrePushLocalBackupUtcPrefix",
            "LatestPrePushCloudBackupUtcPrefix",
            "ImportantLocalSaveEvidenceCountPrefix",
            "BaselineManualPushPrerequisitesSatisfiedPrefix",
            "ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Safety.cs" `
        "requires completed selected-branch Push evidence after branch switches" `
        @(
            "LastManualPushIsAfterBranchSwitch",
            "LastManualPushMatchesSelectedBranch",
            "HasManualPushAfterBranchSwitch",
            'LatestManualPushEvidenceOutcome\(dataDir\), "completed"',
            "LastManualPushCompletionRecorded",
            "LastManualPushPrePushBackupEvidenceSatisfied",
            "SteamGameBranch\.Normalize"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Write.cs" `
        "records successful manual Push evidence after save-origin and backup gates" `
        @(
            "WriteManualPushMarker",
            "LauncherSaveOriginEvidence\.WriteManualPushOrigin",
            "PrePushLocalBackupEvidenceCountPrefix",
            "PrePushCloudBackupEvidenceCountPrefix",
            "LatestPrePushLocalBackupUtcPrefix",
            "LatestPrePushCloudBackupUtcPrefix",
            "ImportantLocalSaveEvidenceCountPrefix",
            "BaselineManualPushPrerequisitesSatisfiedPrefix",
            "BranchSwitchPrePushBackupEvidenceSatisfiedPrefix",
            "ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix",
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.BlockedPush.cs" `
        "records blocked-before-upload manual Push evidence" `
        @(
            "LastManualPushBlockedUtc",
            "LastManualPushBlockedUtcParseable",
            "LastManualPushBlockedSelectedBranch",
            "LastManualPushBlockedSelectedBranchSelectionKind",
            "LastManualPushBlockedSelectorMode",
            "LastManualPushBlockedSelectedVersion",
            "LastManualPushBlockedSelectedVersionSlotKind",
            "LastManualPushBlockedSelectedVersionSlotDirectory",
            "LastManualPushBlockedRecordedPrerequisitesSatisfied",
            "LastManualPushBlockedRecordedLocalBackupCount",
            "LastManualPushBlockedRecordedCloudBackupCount",
            "LastManualPushBlockedRecordedLatestLocalBackupUtc",
            "LastManualPushBlockedRecordedLatestCloudBackupUtc",
            "LastManualPushBlockedRecordedImportantLocalSaveEvidenceCount",
            "LastManualPushBlockedRecordedBaselinePrerequisitesSatisfied",
            "LastManualPushBlockedRecordedPrePushBackupEvidenceSatisfied",
            "LastManualPushBlockedReason",
            "LastManualPushBlockedBeforeUpload",
            "LastManualPushBlockedMatchesSelectedBranch",
            "WriteManualPushBlockedMarker",
            "WriteManualPushBlockedMarker\(string dataDir, string selectedBranch, string reason\)",
            "ManualPushBlockedBeforeUploadPrefix",
            "ManualPushBlockedReasonPrefix",
            "PrePushLocalBackupEvidenceCountPrefix",
            "PrePushCloudBackupEvidenceCountPrefix",
            "LatestPrePushLocalBackupUtcPrefix",
            "LatestPrePushCloudBackupUtcPrefix",
            "ImportantLocalSaveEvidenceCountPrefix",
            "BaselineManualPushPrerequisitesSatisfiedPrefix",
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Markers.cs" `
        "keeps manual cloud evidence marker parsing isolated" `
        @(
            "HasCompletionFlag",
            'string\? ReadSelectedBranch',
            "ReadMarkerValue",
            "LauncherMarkerFile\.ReadOptionalValue",
            "ReadUtc",
            "LauncherMarkerFile\.ReadUtc",
            "LauncherMarkerFile\.ReadBoolFlag",
            "SanitizeSingleLine"
        )

}
