function Add-SteamVersionSelectionBranchRuntimeMarkerReadinessChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.BranchMarker.cs" `
        "blocks ambiguous non-public caches without branch marker readiness" `
        @(
            "BranchMarkerReady",
            "HasBranchMetadataProblem",
            "LauncherBranchMarkerFields\.Branch",
            "BranchMarkerHasDepotManifestProvenance",
            "BranchMarkerHasInstallSlotProvenance",
            "BranchMarkerHasIntegrityProvenance"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.BranchMarker.Provenance.cs" `
        "checks branch marker depot, integrity, and install-slot provenance through centralized fields" `
        @(
            "BranchMarkerHasDepotManifestProvenance",
            "LauncherBranchMarkerFields\.DepotManifestRow",
            "BranchMarkerHasIntegrityProvenance",
            "LauncherBranchMarkerIntegrityProvenance\.Read",
            "IsComplete",
            "BranchMarkerHasInstallSlotProvenance",
            "LauncherBranchMarkerFields\.InstallSlotKind",
            "LauncherBranchMarkerFields\.InstallSlotDirectory",
            "LauncherAndroidAppPrivatePath\.MarkerPathMatchesExpectedPath"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.BranchIntegrity.cs" `
        "surfaces ambiguous non-public cache integrity evidence" `
        @(
            "BranchIntegritySummary",
            "LauncherBranchMarkerIntegrityProvenance\.Read",
            "provenance\.IsComplete",
            "LauncherBranchMarkerFields\.DepotManifestCount",
            "provenance\.MatchingPublic",
            "provenance\.DifferingFromPublic",
            "provenance\.InheritedFromPublic",
            "provenance\.MissingSelectedManifest",
            "provenance\.WithoutPublicComparison",
            "Selected branch appears partial",
            "inherits public content",
            "Selected branch depot manifests all match public",
            "Depot manifest"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Readiness.cs" `
        "reports readiness failure when selected branch metadata is unsafe" `
        @(
            "ReadinessProblem",
            "HasBranchMetadataProblem"
        )
}
