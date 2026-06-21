function Add-SteamVersionSelectionBranchRuntimeMarkerFieldChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchMarkerFields.cs" `
        "keeps branch marker field prefixes centralized for readiness and integrity checks" `
        @(
            "internal static class LauncherBranchMarkerFields",
            "Branch = ""Branch:""",
            "DepotManifestCount = ""Depot manifest count:""",
            "DepotManifestRow = ""Depot manifest:""",
            "DepotsMatchingPublic = ""Depot manifests matching public count:""",
            "DepotsDifferingFromPublic = ""Depot manifests differing from public count:""",
            "DepotsWithoutPublicComparison = ""Depot manifests without public comparison count:""",
            "DepotsInheritedFromPublic = ""Depot manifests inherited from public count:""",
            "DepotsMissingSelectedManifest = ""Depot manifests missing selected branch manifest count:""",
            "InstallSlotKind = ""Install slot kind:""",
            "InstallSlotDirectory = ""Install slot directory:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherBranchMarkerIntegrityProvenance.cs" `
        "reads typed branch marker integrity provenance for launch gates and diagnostics" `
        @(
            "readonly record struct LauncherBranchMarkerIntegrityProvenance",
            "MatchingPublic",
            "DifferingFromPublic",
            "WithoutPublicComparison",
            "InheritedFromPublic",
            "MissingSelectedManifest",
            "IsComplete",
            "LauncherMarkerFile\.ReadInt",
            "LauncherBranchMarkerFields\.DepotsMatchingPublic",
            "LauncherBranchMarkerFields\.DepotsDifferingFromPublic",
            "LauncherBranchMarkerFields\.DepotsWithoutPublicComparison",
            "LauncherBranchMarkerFields\.DepotsInheritedFromPublic",
            "LauncherBranchMarkerFields\.DepotsMissingSelectedManifest"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherAndroidAppPrivatePath.cs" `
        "centralizes Android app-private path normalization and alias comparisons" `
        @(
            "internal static class LauncherAndroidAppPrivatePath",
            "NormalizePath",
            "NormalizeMarkerPath",
            "MarkerPathMatchesExpectedPath",
            "PathMatchesOrLeftAliasMatches",
            "NormalizedMarkerPathsEqual",
            "AndroidAppPrivatePathAlias",
            "DataUserPrefix = ""/data/user/0/""",
            "DataDataPrefix = ""/data/data/""",
            "sourceRootPrefix",
            "aliasRootPrefix"
        )
}
