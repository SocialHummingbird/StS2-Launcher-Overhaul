function Add-SteamVersionSelectionCloudSafetyBackupPushEnforcementChecks {
    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Transfer.cs" `
        "backs up and verifies every existing destination inside the transfer" `
        @(
            "CreateBackupSessionAsync",
            "\.sts2-launcher/transfer-backups/",
            "BackupDestinationsAsync",
            "DestinationFileExistsAsync",
            "ReadDestinationFileBytesAsync",
            "WriteAndVerifyLocalAsync",
            "RequireHash"
        )

    Add-Check `
        "scripts\test-cloud-sync-production-path.ps1" `
        "runs mandatory transfer-backup behavior through the production-path probe" `
        @(
            "CloudSyncProductionPathProbe\\CloudSyncProductionPathProbe\.csproj",
            "dotnet\.Source run",
            "Non-mutating desktop cloud validation passed"
        )
}
