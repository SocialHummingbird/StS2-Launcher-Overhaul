function Add-SteamVersionSelectionCloudSafetyLocalBackupMirrorChecks {
    Add-Check `
        "src\STS2Mobile\Steam\LocalSaveBackupPlan.cs" `
        "keeps automatic local backups path-preserving and recovery conservative" `
        @(
            'CurrentDirectoryName = "Current"',
            'HistoryDirectoryName = "History"',
            'MaxHistoryGenerations = 20',
            'IsBackupEligible',
            'ShouldRestoreMissing',
            'profile\.save',
            'progress\.save',
            'TryResolveUnderRoot',
            'segment is "\." or "\.\."'
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.LocalMirror.cs" `
        "mirrors local saves, archives changed content, and restores only missing stable files" `
        @(
            "RefreshLocalMirror",
            "RestoreMissingStableFiles",
            "MirrorLocalWrite",
            "local\.FileExists\(relativePath\) && local\.GetFileSize\(relativePath\) > 0",
            "TryArchiveMirrorFile",
            "WriteMirrorFile",
            "PruneLocalMirrorHistory",
            "lock \(LocalMirrorGate\)",
            "SavePathDiscovery\.Get\(\s*local,\s*cancellationToken",
            "CancellableSaveStore\.ReadFileAsync",
            "CancellableAtomicFile\.WriteAllTextAsync",
            "CancellableAtomicFile\.WriteAllBytesAsync",
            "catch \(OperationCanceledException\)",
            "Automatic local backup refresh"
        )

    Add-Check `
        "src\STS2Mobile\Steam\AndroidLocalSaveStore.FileIo.cs" `
        "mirrors each successful Android game-save write without waiting for another launcher start" `
        @(
            "File\.WriteAllBytes\(fullPath, bytes\)",
            "CloudSyncCoordinator\.MirrorLocalSaveWrite\(path, bytes\)"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.Paths.cs" `
        "keeps vanilla and modded legacy backup namespaces distinct" `
        @(
            'string\.Equals\(',
            'parts\[0\]',
            '"modded"',
            'Path\.Combine\("modded", profileDir\)'
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSavePath.cs" `
        "classifies root vanilla and modded profile metadata as important backup content" `
        @(
            'ProfileSaveFile = "profile\.save"',
            'canonLowerPath == ProfileSaveFile',
            'EndsWith\(\$"/\{ProfileSaveFile\}"\)'
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPreferences.LocalBackup.cs" `
        "refreshes and recovers the local save mirror when the enabled preference is applied" `
        @(
            "EnsureExternalDirectories",
            "RefreshLocalBackup\(\s*restoreMissing: true\s*\)",
            "SaveLocalBackupEnabledWithResult",
            "ApplyLocalBackupWithResult"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.SaveModePlan.cs" `
        "reapplies automatic local backup before game SaveManager initialization" `
        @(
            "LoadAndApplyLocalBackupEnabled\(\)",
            "LoadAndApplyCloudSyncEnabled\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLaunchCoordinator.Attempt.cs" `
        "refreshes the local save mirror and recovers stable files before game handoff" `
        @(
            "RefreshLocalBackup\(\s*restoreMissing: true\s*\)",
            "_localBackupRecoveryCompleted"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.ViewEvents.cs" `
        "publishes local-backup toggle recovery results to the cloud state refresher" `
        @(
            "LocalBackupToggled",
            "_session\.LocalBackupToggled\(pressed\)",
            "_cloud\.LocalBackupRecoveryCompleted\(result\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherController.cs" `
        "publishes pre-launch recovery results without replacing ordinary launch status" `
        @(
            "result => _cloud\.LocalBackupRecoveryCompleted",
            "reportNoChanges: false"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherCloudSyncCoordinator.StateRefresh.cs" `
        "recaptures save, mirror, and Upload eligibility state after recovery" `
        @(
            "CaptureCurrentState",
            "CountImportantSaveEvidence",
            "importantSaveCount > 0",
            "CurrentMirrorSaveCount",
            "ApplyCloudPostOperationSnapshot",
            "LocalBackupRecoveryPresentation\.Create"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Pull.cs" `
        "refreshes automatic local backup after cloud Pull and modded save seeding" `
        @(
            "seedSession\.CompleteAsync",
            "sync\.RefreshLocalBackupMirror\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.CurrentMirror.cs" `
        "surfaces automatic local backup count and latest write evidence" `
        @(
            "CurrentMirrorSaveCount",
            "LatestCurrentMirrorWriteUtc",
            "LocalSaveBackupPlan\.IsBackupEligible"
        )
}
