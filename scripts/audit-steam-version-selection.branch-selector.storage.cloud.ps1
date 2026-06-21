function Add-SteamVersionSelectionBranchSelectorStorageCloudPreferenceChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.LocalBackup.cs" `
        "keeps local-backup preference effects isolated from branch selection" `
        @(
            "SaveLocalBackupEnabled",
            "RequestStoragePermissionForLocalBackup",
            "AppPaths\.HasStoragePermission",
            "AppPaths\.RequestStoragePermission",
            "CloudSyncCoordinator\.SetLocalBackupEnabled",
            "AppPaths\.EnsureExternalDirectories"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.CloudSync.cs" `
        "keeps cloud-sync preference application isolated from branch selection" `
        @(
            "LoadAndApplyCloudSyncEnabled",
            "SaveCloudSyncEnabled",
            "ApplyCloudSync",
            "LauncherCloudSaveState\.SetCloudSyncEnabled"
        )
}
