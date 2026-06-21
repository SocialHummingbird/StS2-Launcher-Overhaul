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
