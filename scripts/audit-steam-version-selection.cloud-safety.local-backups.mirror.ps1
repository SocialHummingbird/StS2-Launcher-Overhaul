function Add-SteamVersionSelectionCloudSafetyLocalBackupMirrorChecks {
    Add-Check `
        "src\STS2Mobile\Steam\LocalSaveBackupPlan.cs" `
        "keeps automatic local backups path-preserving without restore policy" `
        @(
            'CurrentDirectoryName = "Current"',
            'HistoryDirectoryName = "History"',
            'MaxHistoryGenerations = 20',
            'LauncherMetadataDirectoryName = "\.sts2-launcher"',
            'IsBackupEligible',
            'lower == LauncherMetadataDirectoryName',
            'lower\.StartsWith\(',
            'TryResolveUnderRoot',
            'segment is "\." or "\.\."'
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SavePathDiscovery.Enumeration.cs" `
        "does not enumerate launcher transfer backups or sentinels as game saves" `
        @(
            "IgnoredEnumerationDirectories",
            "LocalSaveBackupPlan\.LauncherMetadataDirectoryName",
            "ShouldSkipEnumeratedDirectory"
        )

    Add-Check `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.LocalMirror.cs" `
        "mirrors local saves and archives changed content without mutating local saves" `
        @(
            "RefreshLocalMirror",
            "MirrorLocalWrite",
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

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.LocalMirror.cs" `
        "keeps the local mirror backup-only so transfer tombstones cannot be resurrected" `
        @(
            "restoreMissing",
            "RestoreMissingStableFiles",
            "ShouldRestoreMissing",
            "local\.WriteFile\(relativePath"
        )

    Add-Check `
        "src\STS2Mobile\Steam\AndroidLocalSaveStore.FileIo.cs" `
        "transactionally stores and mirrors each successful Android game-save write" `
        @(
            "CancellableAtomicFile\.WriteAllBytesAsync",
            "overwrite: true",
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
        "refreshes the backup-only local save mirror when the enabled preference is applied" `
        @(
            "EnsureExternalDirectories",
            "RefreshLocalBackup\(\)",
            "SaveLocalBackupEnabledWithResult",
            "ApplyLocalBackupWithResult"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherPreferences.LocalBackup.cs" `
        "does not request automatic restoration when local backup is enabled" `
        @(
            "restoreMissing"
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
        "refreshes the backup-only local save mirror before game handoff" `
        @(
            "RefreshLocalBackup\(\)",
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
            "CloudPushSafetyContext\.Create",
            "EvaluateCloudPushEligibility",
            "CurrentMirrorSaveCount",
            "ApplyCloudPostOperationSnapshot",
            "LocalBackupRecoveryPresentation\.Create"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Transfer.cs" `
        "keeps trustworthy transfer independent of optional mirror refresh and cross-namespace seeding" `
        @(
            "RefreshLocalBackupMirror",
            "RefreshLocalMirror",
            "ModdedSaveSeed",
            "SeedModded",
            "SavePathDiscovery"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.CurrentMirror.cs" `
        "surfaces automatic local backup count and latest write evidence" `
        @(
            "CurrentMirrorSaveCount",
            "LatestCurrentMirrorWriteUtc",
            "LocalSaveBackupPlan\.IsBackupEligible"
        )

    Add-Check `
        "tools\LocalSaveRecoveryProductionPathProbe\Program.cs" `
        "covers backup mirroring, tombstone preservation, archival, and corrupt-mirror isolation" `
        @(
            "eligible saves mirror into an isolated tree",
            "LauncherTransferBackupPath",
            "!File\.Exists\(MirrorPath\(root, LauncherTransferBackupPath\)\)",
            "transfer tombstones remain deleted during backup refresh",
            "newer local saves replace the mirror and archive old bytes",
            "corrupt and partial mirror files never restore local saves",
            "ReadCount",
            "WriteCount",
            "no credentials, network store, hardware, Steam Cloud operation",
            "or automatic save restoration was used"
        )

    Add-Check `
        "scripts\test-local-save-recovery-production-path.ps1" `
        "provides a repeatable non-mutating local-save backup-only validation entry point" `
        @(
            "LocalSaveRecoveryProductionPathProbe\.csproj",
            "temporary trees and in-memory stores",
            "No automatic restore or Steam Cloud operation was performed"
        )
}
