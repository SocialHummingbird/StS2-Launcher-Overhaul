function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchBackupChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Backup.cs" `
        "reports backup storage and pre-Push backup evidence for branch-switch safety" `
        @(
            "AppendBranchSwitchBackupEvidence",
            "Important Android local save evidence count in bounded scan",
            "Important Android local save evidence present",
            "Backup storage permission available",
            "Backup storage directory",
            "Backup storage directory exists",
            "Branch-switch manual Push prerequisites satisfied",
            "Pre-Push local backup evidence count",
            "Pre-Push cloud backup evidence count",
            "Latest pre-Push local backup UTC",
            "Latest pre-Push cloud backup UTC",
            "Pre-Push local backup evidence after branch switch",
            "Pre-Push cloud backup evidence after branch switch",
            "Branch-switch pre-Push backup evidence satisfied"
        )
}
