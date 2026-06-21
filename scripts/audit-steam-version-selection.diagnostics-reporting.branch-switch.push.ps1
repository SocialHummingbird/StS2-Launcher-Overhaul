function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchPushChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Push.cs" `
        "reports Manual Push evidence for branch-switch Push safety" `
        @(
            "AppendManualPushEvidence",
            "Manual Push evidence marker filename",
            "Manual Push evidence marker path",
            "Manual Push evidence marker present",
            "Manual Push evidence UTC",
            "Latest manual Push evidence outcome",
            "Latest manual Push evidence UTC",
            "Latest manual Push evidence selected branch",
            "Latest manual Push evidence selected branch selection kind",
            "Latest manual Push evidence selector mode",
            "Latest manual Push evidence selected version",
            "Latest manual Push evidence selected version slot kind",
            "Latest manual Push evidence selected version slot directory",
            "Latest manual Push evidence reason",
            "Manual Push evidence UTC parseable",
            "Manual Push evidence selected branch",
            "Manual Push evidence selected version",
            "Manual Push evidence selected version slot kind",
            "Manual Push evidence selected version slot directory",
            "Manual Push evidence recorded local backup count",
            "Manual Push evidence recorded cloud backup count",
            "Manual Push evidence recorded latest local backup UTC",
            "Manual Push evidence recorded latest cloud backup UTC",
            "Manual Push evidence recorded important local save evidence count",
            "Manual Push evidence recorded baseline prerequisites satisfied",
            "Manual Push completion flag recorded",
            "Manual Push evidence is after branch switch",
            "Manual Push evidence matches selected branch",
            "Manual Push evidence recorded pre-Push backup evidence satisfied",
            "Manual Push completed after branch switch for selected version with backup evidence",
            "LatestManualPushEvidenceOutcome"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.BlockedPush.cs" `
        "reports blocked Manual Push evidence for branch-switch Push safety" `
        @(
            "AppendManualPushBlockedEvidence",
            "Manual Push blocked evidence marker filename",
            "Manual Push blocked evidence marker path",
            "Manual Push blocked evidence marker present",
            "Manual Push blocked evidence UTC",
            "Manual Push blocked evidence UTC parseable",
            "Manual Push blocked evidence selected branch",
            "Manual Push blocked evidence selected version",
            "Manual Push blocked evidence selected version slot kind",
            "Manual Push blocked evidence selected version slot directory",
            "Manual Push blocked evidence matches selected branch",
            "Manual Push blocked evidence recorded prerequisites satisfied",
            "Manual Push blocked evidence recorded local backup count",
            "Manual Push blocked evidence recorded cloud backup count",
            "Manual Push blocked evidence recorded latest local backup UTC",
            "Manual Push blocked evidence recorded latest cloud backup UTC",
            "Manual Push blocked evidence recorded important local save evidence count",
            "Manual Push blocked evidence recorded baseline prerequisites satisfied",
            "Manual Push blocked evidence recorded pre-Push backup evidence satisfied",
            "Manual Push blocked evidence reason",
            "Manual Push blocked before upload evidence recorded"
        )
}
