function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchMarkerChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.Marker.cs" `
        "reports branch-switch marker safety state" `
        @(
            "AppendBranchSwitchMarkerEvidence",
            "Branch switch marker filename",
            "Branch switch marker path",
            "Branch switch marker present",
            "Branch switch marker UTC",
            "Branch switch marker UTC parseable",
            "Branch switch previous branch",
            "Branch switch selected branch",
            "Branch switch selected branch selection kind",
            "Branch switch selector mode",
            "Branch switch selected version",
            "Branch switch selected version slot kind",
            "Branch switch selected version slot directory",
            "Branch switch selected branch matches current selected branch",
            "Branch switch selected branch note",
            "Branch switch local backup forced",
            "Branch switch manual Push requires backup storage",
            "Branch switch warning acknowledged",
            "Branch switch non-public warning acknowledged",
            "Branch switch marker has required safety evidence",
            "Branch switch marker has required safety evidence for selected branch",
            "Push requires backup storage after branch switch"
        )
}
