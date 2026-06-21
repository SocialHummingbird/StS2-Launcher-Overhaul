function Add-SteamVersionSelectionCloudSafetyLocalBackupChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.cs" `
        "exposes bounded local save evidence counts for branch-switch Push safety" `
        @(
            "internal static partial class LauncherLocalSaveEvidence",
            "MaxFilesToInspect = 1000",
            "MaxDirectoriesToInspect = 250",
            "IgnoredDirectoryNames",
            "HasImportantSaveEvidence",
            "CountImportantSaveEvidence"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.Classify.cs" `
        "classifies non-empty Android save artifacts as important local save evidence" `
        @(
            "IsImportantSaveEvidence",
            "Path\.GetRelativePath",
            "ToLowerInvariant",
            "FileHasContent",
            "\.save",
            "\.save\.backup",
            "\.run",
            "\.bak",
            "prefs",
            "prefs\.save",
            "prefs\.backup",
            "prefs\.save\.backup",
            "new FileInfo\(file\)\.Length > 0"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.Enumeration.cs" `
        "walks local save directories with runtime-directory exclusions and scan limits" `
        @(
            "EnumerateFilesSafely",
            "new Stack<string>",
            "MaxFilesToInspect",
            "MaxDirectoriesToInspect",
            "IsIgnoredRuntimeDirectory",
            "IgnoredDirectoryNames",
            "StringComparison\.OrdinalIgnoreCase",
            "SafeFiles",
            "SafeDirectories"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherLocalSaveEvidence.FileSystem.cs" `
        "swallows filesystem enumeration failures when scanning local save evidence" `
        @(
            "SafeFiles",
            "Directory\.GetFiles",
            "SafeDirectories",
            "Directory\.GetDirectories",
            "Array\.Empty<string>\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.cs" `
        "exposes local/cloud pre-Push backup evidence after branch switching" `
        @(
            "internal static partial class LauncherBackupEvidence",
            "local-pre-push",
            "cloud-pre-push",
            "MaxBackupFilesToInspect",
            "LocalPrePushBackupCount",
            "CloudPrePushBackupCount",
            "LatestLocalPrePushBackupUtc",
            "LatestCloudPrePushBackupUtc",
            "HasLocalPrePushBackupAfterBranchSwitch",
            "HasCloudPrePushBackupAfterBranchSwitch",
            "HasPrePushBackupEvidenceAfterBranchSwitch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.Counts.cs" `
        "counts pre-Push backup files through bounded backup enumeration" `
        @(
            "CountBackups",
            "EnumerateBackups\(source\)",
            "count\+\+"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.BranchSwitch.cs" `
        "compares pre-Push backup timestamps against branch-switch evidence" `
        @(
            "HasBackupAfterBranchSwitch",
            "TryReadBranchSwitchUtc",
            "LauncherBranchSwitchSafety\.MarkerUtc",
            "DateTimeStyles\.AdjustToUniversal",
            "TryReadBackupUtc\(backupPath, source, out var backupUtc\)",
            "backupUtc >= branchSwitchUtc"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.Enumeration.cs" `
        "enumerates pre-Push backup files without walking unbounded backup trees" `
        @(
            "EnumerateBackups",
            "Directory\.Exists\(BackupDirectory\)",
            "Array\.Empty<string>\(\)",
            "Directory\.EnumerateFiles",
            "\*\.\{source\}\.bak",
            "SearchOption\.AllDirectories",
            "inspected >= MaxBackupFilesToInspect"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBackupEvidence.Timestamp.cs" `
        "parses backup UTC evidence from backup filenames before falling back to file mtime" `
        @(
            "LatestBackupUtc",
            "TryReadBackupUtc",
            "ToString\(""O"", CultureInfo\.InvariantCulture\)",
            "<none>",
            "EndsWith\(suffix, StringComparison\.OrdinalIgnoreCase\)",
            "DateTimeOffset\.FromUnixTimeSeconds",
            "File\.GetLastWriteTimeUtc"
        )

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
