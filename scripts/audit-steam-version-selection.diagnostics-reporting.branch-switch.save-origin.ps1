function Add-SteamVersionSelectionDiagnosticsReportingBranchSwitchSaveOriginChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchSwitchSafety.SaveOrigin.cs" `
        "reports Android save-origin and current local-save evidence for branch-switch Push safety" `
        @(
            "AppendCurrentLocalSaveEvidence",
            "Current important Android local save evidence count",
            "Current important Android local save evidence present",
            "AppendSaveOriginEvidence",
            "Android save-origin marker filename",
            "Android save-origin marker path",
            "Android save-origin marker present",
            "Android save-origin selected runtime slot ID",
            "Android save-origin selected PCK SHA256",
            "Android save-origin selected source sts2\.dll SHA256",
            "Android save-origin selected runtime playable at origin",
            "Android save-origin matches selected branch",
            "Android save-origin selected runtime slot ID matches current runtime",
            "Android save-origin current selected runtime is playable",
            "Android local saves verified for selected branch",
            "Android local saves verified for selected runtime",
            "Baseline manual Push prerequisites satisfied"
        )
}
