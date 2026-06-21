function Add-SteamVersionSelectionActionSupportDiagnosticsChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Construction.Support.DiagnosticsTools.cs" `
        "labels launcher support log copy as review-before-sharing" `
        @(
            "Copy Launcher Log \(Review First\)",
            "Create Help Report",
            "Show Last Problem"
        )
}
