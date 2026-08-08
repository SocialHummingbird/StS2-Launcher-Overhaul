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
        "reads completed manual Push metadata without obsolete prerequisite flags" `
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
            "PrePushLocalBackupEvidenceCountPrefix",
            "PrePushCloudBackupEvidenceCountPrefix",
            "LatestPrePushLocalBackupUtcPrefix",
            "LatestPrePushCloudBackupUtcPrefix",
            "ImportantLocalSaveEvidenceCountPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Safety.cs" `
        "reports selected-branch Push history after branch switches" `
        @(
            "LastManualPushIsAfterBranchSwitch",
            "LastManualPushMatchesSelectedBranch",
            "SteamGameBranch\.Normalize"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Write.cs" `
        "records successful manual Push history without prerequisite verdicts" `
        @(
            "WriteManualPushMarker",
            "LauncherSaveOriginEvidence\.WriteManualPushOrigin",
            "PrePushLocalBackupEvidenceCountPrefix",
            "PrePushCloudBackupEvidenceCountPrefix",
            "LatestPrePushLocalBackupUtcPrefix",
            "LatestPrePushCloudBackupUtcPrefix",
            "ImportantLocalSaveEvidenceCountPrefix",
            "SelectedVersionPrefix",
            "SelectedBranchNotePrefix"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Push.Write.cs" `
        "does not relabel successful Push history as satisfaction of obsolete gates" `
        @(
            "PrerequisitesSatisfied",
            "PrePushBackupEvidenceSatisfied",
            "CompletedAfterBranchSwitchSafetyGates"
        )
}
