function Add-SteamVersionSelectionAuthCloudSafetyBackupBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.local-backups.ps1" `
        "keeps local-save and pre-Push backup evidence audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyLocalBackupChecks",
            "audit-steam-version-selection.cloud-safety.local-backups.local-saves.ps1",
            "audit-steam-version-selection.cloud-safety.local-backups.backup-evidence.ps1",
            "audit-steam-version-selection.cloud-safety.local-backups.push-enforcement.ps1",
            "Add-SteamVersionSelectionCloudSafetyLocalSaveEvidenceChecks",
            "Add-SteamVersionSelectionCloudSafetyBackupEvidenceChecks",
            "Add-SteamVersionSelectionCloudSafetyBackupPushEnforcementChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.local-backups.local-saves.ps1" `
        "keeps local save evidence audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyLocalSaveEvidenceChecks",
            "LauncherLocalSaveEvidence.cs",
            "LauncherLocalSaveEvidence.Classify.cs",
            "LauncherLocalSaveEvidence.Enumeration.cs",
            "LauncherLocalSaveEvidence.FileSystem.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.local-backups.backup-evidence.ps1" `
        "keeps pre-Push backup evidence audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyBackupEvidenceChecks",
            "LauncherBackupEvidence.cs",
            "LauncherBackupEvidence.BranchSwitch.cs",
            "LauncherBackupEvidence.Enumeration.cs",
            "LauncherBackupEvidence.Timestamp.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.local-backups.push-enforcement.ps1" `
        "keeps pre-Push backup enforcement audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyBackupPushEnforcementChecks",
            "CloudSyncCoordinator.SaveBackups.Manual.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.startup-context.ps1" `
        "keeps branch-switch startup context audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyStartupContextChecks",
            "LauncherController.Startup.BranchSwitch.cs",
            "LauncherController.Startup.RuntimeEvidence.cs"
        )
}
