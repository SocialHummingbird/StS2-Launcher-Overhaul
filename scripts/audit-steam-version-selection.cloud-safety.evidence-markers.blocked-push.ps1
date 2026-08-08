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
            "LastManualPushBlockedRecordedLocalBackupCount",
            "LastManualPushBlockedRecordedCloudBackupCount",
            "LastManualPushBlockedRecordedLatestLocalBackupUtc",
            "LastManualPushBlockedRecordedLatestCloudBackupUtc",
            "LastManualPushBlockedRecordedImportantLocalSaveEvidenceCount",
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
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.BlockedPush.cs" `
        "does not record obsolete Pull, branch, backup, or storage prerequisite verdicts" `
        @(
            "ManualPushPrerequisitesSatisfied",
            "BaselineManualPushPrerequisitesSatisfied",
            "PrePushBackupEvidenceSatisfied"
        )
}
