function Add-SteamVersionSelectionDiagnosticsReportingBranchAvailabilityChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchAvailability.cs" `
        "reports selected Steam branch availability state" `
        @(
            "Steam branch availability marker filename",
            "Steam branch availability marker path",
            "Steam branch availability marker present",
            "Steam branch availability UTC",
            "Steam branch availability selected branch",
            "Steam branch availability matches current selected branch",
            "Steam branch availability selected branch visibility",
            "Steam branch availability selected branch Windows depot manifests",
            "Steam branch availability selected branch downloadable",
            "Steam branch availability selected branch problem",
            "Steam branch availability visible branch count",
            "Steam branch availability visible branches"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchAvailability.Marker.cs" `
        "parses selected Steam branch availability marker state" `
        @(
            "ReadBranchAvailabilityMarkerValue",
            "ReadBranchAvailabilityMarkerValues",
            "BranchAvailabilityMarkerMatchesSelectedBranch",
            "BranchAvailabilitySelectedBranchDownloadable",
            "BranchAvailabilitySelectedBranchProblem",
            "BranchAvailabilitySelectedBranchManifestCount",
            "BranchAvailabilitySelectedBranchPasswordProtected",
            "SteamBranchAvailabilityMarkerFile\.ReadValue",
            "SteamBranchAvailabilityMarkerFile\.ReadValues",
            "SteamBranchAvailabilityMarkerFile\.ReadVisibleRows",
            "SteamBranchAvailabilityMarkerFile\.Exists",
            "LauncherMarkerFile\.ReadFailedValue",
            "BranchMatches",
            "PasswordProtected",
            "selected branch is password-protected"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherDiagnostics.ReportBranchAvailability.Marker.cs" `
        "does not duplicate Steam branch availability visible-row parsing" `
        @(
            'IndexOf\(" \["',
            "BranchAvailabilityMarkerValueMatchesBranch",
            "PasswordRequiredTrueToken",
            "LauncherMarkerFile\.ReadJoinedValues",
            "LauncherMarkerFile\.ReadValues"
        )
}
