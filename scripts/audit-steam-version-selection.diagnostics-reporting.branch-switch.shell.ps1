function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchShellChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.cs" `
        "orchestrates branch-switch cloud-sync and backup safety diagnostics" `
        @(
            "AppendBranchSwitchSafety",
            "selectedBranch = LauncherPreferences\.ReadGameBranch\(\)",
            "importantSaveEvidenceCount",
            "AppendBranchSwitchMarkerEvidence",
            "AppendManualPullEvidence",
            "AppendCurrentLocalSaveEvidence",
            "AppendSaveOriginEvidence",
            "AppendManualPushEvidence",
            "AppendManualPushBlockedEvidence",
            "AppendBranchSwitchBackupEvidence"
        )
}
