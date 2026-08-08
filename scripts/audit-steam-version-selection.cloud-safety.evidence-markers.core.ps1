function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerCoreChecks {
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
            "ManualPullOutcomePrefix = ""Manual Pull outcome:""",
            "ManualPullOutcomeDetailPrefix = ""Manual Pull outcome detail:""",
            "PrePushLocalBackupEvidenceCountPrefix = ""Pre-Push local backup evidence count:""",
            "PrePushCloudBackupEvidenceCountPrefix = ""Pre-Push cloud backup evidence count:""",
            "LatestPrePushLocalBackupUtcPrefix = ""Latest pre-Push local backup UTC:""",
            "LatestPrePushCloudBackupUtcPrefix = ""Latest pre-Push cloud backup UTC:""",
            "ImportantLocalSaveEvidenceCountPrefix = ""Important Android local save evidence count:""",
            "BlockedReasonPrefix = ""Blocked reason:""",
            "ManualPushBlockedBeforeUploadPrefix = ""Manual Push blocked before upload:""",
            "ManualPushBlockedReasonPrefix = ""Manual Push blocked:"""
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherCloudSyncEvidence.Fields.cs" `
        "does not encode obsolete Pull-before-Push or branch/storage prerequisite claims" `
        @(
            "ManualPullCompletedBeforePushPrefix",
            "ManualPullCompletedBeforeBranchSwitchPushPrefix",
            "ManualPushPrerequisitesSatisfiedPrefix",
            "BranchSwitchPrePushBackupEvidenceSatisfiedPrefix",
            "ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix"
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
