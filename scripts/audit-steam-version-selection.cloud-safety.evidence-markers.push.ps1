function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPushChecks {
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
}
