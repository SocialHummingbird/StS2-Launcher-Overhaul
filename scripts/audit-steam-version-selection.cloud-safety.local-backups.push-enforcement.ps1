function Add-SteamVersionSelectionCloudSafetyBackupPushEnforcementChecks {
    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.Manual.cs" `
        "fails manual Push before upload when required backup evidence is missing" `
        @(
            "EnforceManualPushBackupEvidence",
            "Manual Push blocked: local backup is enabled but backup storage permission is unavailable",
            "Manual Push blocked: local pre-Push backup evidence is incomplete",
            "Manual Push blocked: cloud pre-Push backup evidence is incomplete",
            "CloudImportantSaveCount",
            "importantPaths.Count",
            "localBackups < importantPaths.Count",
            "cloudBackups < cloudImportantSaveCount",
            "AppPaths\.HasStoragePermission"
        )
}
