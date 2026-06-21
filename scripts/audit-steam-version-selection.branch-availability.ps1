function Add-SteamVersionSelectionBranchAvailabilityChecks {
    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Builder.cs" `
        "parses Steam branch metadata including beta password flags" `
        @(
            "BranchAvailabilityBuilder",
            "pwdrequired",
            "password_required"
        )

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
        "src\STS2Mobile\Steam\SteamBranchAvailabilityMarkerFields.cs" `
        "centralizes Steam branch availability marker prefixes and row metadata keys" `
        @(
            "Utc = ""UTC:""",
            "SelectedBranch = ""Selected branch:""",
            "SelectedBranchVisibility = ""Selected branch visibility:""",
            "SelectedBranchWindowsDepotManifests = ""Windows depot manifests for selected branch:""",
            "VisibleBranchCount = ""Visible branch count:""",
            "VisibleBranch = ""Visible branch:""",
            "VisibleBranchOverflowCount = ""Visible branch overflow count:""",
            "WindowsManifestDepotsKey = ""windowsManifestDepots""",
            "MetadataVisibleKey = ""metadataVisible""",
            "PasswordRequiredKey = ""passwordRequired""",
            "BuildIdKey = ""buildId""",
            "DescriptionKey = ""description""",
            "PasswordRequiredTrueToken = PasswordRequiredKey \+ ""=true""",
            "ZeroWindowsManifestDepotsToken = WindowsManifestDepotsKey \+ ""=0"""
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamBranchAvailabilityMarkerRow.cs" `
        "parses Steam branch availability visible-branch marker rows once for launcher consumers" `
        @(
            "SteamBranchAvailabilityMarkerRow",
            "Parse\(string value\)",
            "ParseMetadata",
            "BranchMatches",
            "MetadataVisible",
            "WindowsManifestDepotCount",
            "HasWindowsManifestDepotCount",
            "PasswordRequired",
            "PasswordProtected",
            "DownloadableOrUnspecified",
            "SteamBranchAvailabilityMarkerFields\.MetadataVisibleKey",
            "SteamBranchAvailabilityMarkerFields\.WindowsManifestDepotsKey",
            "SteamBranchAvailabilityMarkerFields\.PasswordRequiredKey",
            "SteamBranchAvailabilityMarkerFields\.BuildIdKey",
            "SteamBranchAvailabilityMarkerFields\.DescriptionKey",
            "SteamGameBranch\.Normalize"
        )

    Add-Check `
        "src\STS2Mobile\Steam\SteamBranchAvailabilityMarkerFile.cs" `
        "centralizes Steam branch availability marker path reads and visible-row projection" `
        @(
            "SteamBranchAvailabilityMarkerFile",
            "MarkerPath",
            "Exists",
            "ReadAllLines",
            "ReadValue\(IEnumerable<string> lines, string prefix\)",
            "ReadValues\(IEnumerable<string> lines, string prefix\)",
            "ReadValue\(",
            "missingFileValue",
            "missingLineValue",
            "readFailedValue",
            "ReadValues\(",
            "maxValues",
            "ReadVisibleRows\(IEnumerable<string> lines\)",
            "ReadVisibleRows\(",
            "SteamGameInstallPaths\.BranchAvailabilityMarkerPath",
            "SteamBranchAvailabilityMarkerFields\.VisibleBranch",
            "SteamBranchAvailabilityMarkerRow\.Parse"
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

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Report.cs" `
        "builds account-visible Steam branch availability from app info" `
        @(
            "BranchAvailabilityReport",
            "Visible Steam branches",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchVisibility",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchWindowsDepotManifests",
            "visible branches",
            "DepotIsWindowsCompatible"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Marker.cs" `
        "persists Steam branch availability marker evidence" `
        @(
            "WriteMarker",
            "BranchAvailabilityMarkerPath",
            "MaxBranchAvailabilityMarkerBranches",
            "SteamBranchAvailabilityMarkerFields\.VisibleBranchOverflowCount",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchVisibility",
            "SteamBranchAvailabilityMarkerFields\.SelectedBranchWindowsDepotManifests"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.BranchAvailability.Model.cs" `
        "formats branch availability marker values safely" `
        @(
            "MaxBranchAvailabilityMarkerValueLength",
            "SafeMarkerValue",
            "SteamBranchAvailabilityMarkerFields\.PasswordRequiredKey",
            "DownloadabilityText",
            "password-protected",
            "!PasswordRequired\.Equals\(""true""",
            "SteamBranchAvailabilityMarkerFields\.WindowsManifestDepotsKey",
            "SteamBranchAvailabilityMarkerFields\.MetadataVisibleKey"
        )
}
