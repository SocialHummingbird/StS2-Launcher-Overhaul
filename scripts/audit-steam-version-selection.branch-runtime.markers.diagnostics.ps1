function Add-SteamVersionSelectionBranchRuntimeMarkerDiagnosticsChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportLauncherPreferences.cs" `
        "surfaces partial Steam branch and inherited-public depot evidence" `
        @(
            "Selected game branch marker depots matching public",
            "Selected game branch marker depots differing from public",
            "Selected game branch marker depots without public comparison",
            "Selected game branch marker depots inherited from public",
            "Selected game branch marker depots missing selected branch manifest",
            "Selected game branch marker partial Steam branch evidence",
            "Selected game branch marker depot manifest rows"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchMarkers.cs" `
        "parses partial Steam branch and inherited-public depot marker evidence" `
        @(
            "BranchMarkerPartialSteamBranchEvidence",
            "ReadBranchMarkerValues",
            "LauncherMarkerFile\.ReadJoinedValues",
            "LauncherMarkerFile\.CountLines",
            "LauncherMarkerFile\.ReadInt",
            "selected branch inherits public depot manifests"
        )
}
