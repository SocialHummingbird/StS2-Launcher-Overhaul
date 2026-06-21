function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerBlockedPushChecks {
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
}
