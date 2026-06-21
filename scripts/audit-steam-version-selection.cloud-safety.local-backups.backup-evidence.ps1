function Add-SteamVersionSelectionCloudSafetyBackupEvidenceChecks {
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
}
