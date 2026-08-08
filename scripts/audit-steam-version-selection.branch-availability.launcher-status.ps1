function Add-SteamVersionSelectionBranchAvailabilityLauncherStatusChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.cs" `
        "orchestrates compact Steam branch availability diagnoses in launcher failure status" `
        @(
            "BranchAvailabilityMarkerPath",
            "CompactFailureMessage",
            "Clear",
            "Failed to clear Steam branch availability marker",
            "ReadDiagnosis",
            "RemoveRawBranchAvailabilitySummary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Fields.cs" `
        "keeps compact Steam branch availability constants bound to shared marker fields" `
        @(
            "RawSelectedBranchVisibilitySummaryMarker",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchVisibility",
            "MaxVisibleBranchesInStatus",
            """ "" \+ SteamBranchAvailabilityMarkerFields\.SelectedBranchVisibility"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Read.cs" `
        "reads Steam branch availability markers for launcher failure diagnosis" `
        @(
            "ReadDiagnosis",
            "SteamBranchAvailabilityMarkerFile\.Exists",
            "SteamBranchAvailabilityMarkerFile\.ReadAllLines",
            "SteamBranchAvailabilityMarkerFile\.ReadValue",
            "SteamBranchAvailabilityMarkerFile\.ReadVisibleRows",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranch",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchVisibility",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchWindowsDepotManifests",
            "SteamBranchAvailabilityMarkerFields\.VisibleBranchOverflowCount",
            "MarkerBranchMatchesCurrentSelection",
            "MarkerValueMatchesBranch",
            "VisibleBranchStatus",
            "SelectedStatus",
            "Branch availability:"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Read.cs" `
        "uses the shared Steam branch availability marker file helper instead of direct file reads" `
        @(
            "BranchAvailabilityMarkerPath",
            "\bFile\.ReadAllLines",
            "\bFile\.ReadLines"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Format.cs" `
        "formats compact Steam branch availability user-facing status" `
        @(
            "SelectedStatus",
            "VisibleBranchStatus\(SteamBranchAvailabilityMarkerRow row\)",
            "MarkerValuePasswordProtected",
            "SteamBranchAvailabilityMarkerRow selectedBranchMarker",
            "row\.PasswordProtected",
            "row\.DownloadableOrUnspecified",
            "password-protected",
            "Steam beta password entry is supported",
            "no Windows manifest",
            "RemoveRawBranchAvailabilitySummary",
            "RawSelectedBranchVisibilitySummaryMarker"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Format.cs" `
        "does not duplicate Steam branch availability visible-row parsing" `
        @(
            'IndexOf\(" \["',
            "ZeroWindowsManifestDepotsToken"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Match.cs" `
        "matches Steam branch availability marker rows against the selected branch" `
        @(
            "MarkerBranchMatchesCurrentSelection",
            "LauncherPreferences\.ReadGameBranch",
            "MarkerValueMatchesBranch",
            "MarkerValuePasswordProtected",
            "SteamGameBranch\.Normalize",
            "BranchMatches",
            "PasswordProtected"
        )

    Add-ForbiddenCheck `
        "src\STS2Mobile\Launcher\LauncherBranchAvailabilityStatus.Match.cs" `
        "does not duplicate Steam branch availability visible-row parsing" `
        @(
            'IndexOf\(" \["',
            "PasswordRequiredTrueToken"
        )
}
