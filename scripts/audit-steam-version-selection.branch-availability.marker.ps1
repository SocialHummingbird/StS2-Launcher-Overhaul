function Add-SteamVersionSelectionBranchAvailabilityMarkerChecks {
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

}
