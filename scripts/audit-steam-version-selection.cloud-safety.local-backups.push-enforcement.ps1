function Add-SteamVersionSelectionCloudSafetyBackupPushEnforcementChecks {
    Add-Check `
        "src\STS2Mobile\Steam\ManualPushBackupSafetyPolicy.cs" `
        "fails manual Push before upload when required backup evidence is missing" `
        @(
            "ManualPushBackupSafetyPolicy",
            "EnsureSatisfied",
            "localBackupEnabled",
            "hasStoragePermission",
            "importantLocalSaveCount",
            "localBackupCount",
            "importantCloudSaveCount",
            "cloudBackupCount",
            "Manual Push blocked: local backup is enabled but backup storage permission is unavailable",
            "Manual Push blocked: local pre-Push backup evidence is incomplete",
            "Manual Push blocked: cloud pre-Push backup evidence is incomplete",
            "localBackupCount < importantLocalSaveCount",
            "cloudBackupCount < importantCloudSaveCount"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.Manual.cs" `
        "connects backup counts and storage state to the fail-closed policy before upload" `
        @(
            "EnforceManualPushBackupEvidence",
            "ManualPushBackupSafetyPolicy\.EnsureSatisfied",
            "_localBackupEnabled",
            "AppPaths\.HasStoragePermission",
            "importantPaths\.Count",
            "localBackups",
            "cloudImportantSaveCount",
            "cloudBackups",
            "CloudImportantSaveCount"
        )
}
