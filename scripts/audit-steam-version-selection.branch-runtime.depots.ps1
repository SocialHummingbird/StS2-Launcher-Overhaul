function Add-SteamVersionSelectionBranchRuntimeDepotChecks {
    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.cs" `
        "uses branch-aware depot metadata and writes branch provenance" `
        @(
            "_branch",
            "WriteBranchMarker",
            "DepotManifestReference",
            "Depot manifest",
            "Install slot kind",
            "Install slot directory",
            "SteamGameInstallPaths\.VersionSlotKind",
            "SteamGameInstallPaths\.VersionSlotDirectory",
            "SteamGameInstallPaths\.BranchMarkerPath"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.cs" `
        "writes beta branch integrity and public-inheritance marker evidence" `
        @(
            "Depot manifests matching public count",
            "Depot manifests differing from public count",
            "Depot manifests without public comparison count",
            "Depot manifests inherited from public count",
            "Depot manifests missing selected branch manifest count",
            "selectedBranchManifest=",
            "publicManifest=",
            "manifestSource=",
            "manifestRequestBranch=",
            "selectedMatchesPublic=",
            "effectiveMatchesPublic="
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.Depots.cs" `
        "uses explicit public inheritance for selected branches with missing depot manifests" `
        @(
            "public-inherited",
            "manifestRequestBranch = SteamGameBranch\.Public",
            "has no explicit branch manifest; inheriting public manifest",
            "source=\{manifestSource\}",
            "requestBranch='\{manifestRequestBranch\}'"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.DepotDownload.cs" `
        "requests inherited public manifests against the branch that owns the manifest" `
        @(
            "ManifestRequestBranch",
            "source=\{depot\.ManifestSource\}",
            "requestBranch='\{depot\.ManifestRequestBranch\}'",
            "GetManifestRequestCodeAsync",
            "depot\.ManifestRequestBranch"
        )

    Add-Check `
        "src\STS2Mobile\Steam\DepotDownloader.DepotManifestReference.cs" `
        "tracks selected, public, effective, and request-branch manifest provenance" `
        @(
            "SelectedBranchManifestId",
            "PublicManifestId",
            "ManifestSource",
            "ManifestRequestBranch",
            "HasSelectedBranchManifest",
            "EffectiveMatchesPublicManifest",
            "SelectedBranchManifestMatchesPublic",
            "InheritedFromPublic"
        )

}
