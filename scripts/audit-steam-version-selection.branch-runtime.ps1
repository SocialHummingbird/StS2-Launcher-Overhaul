function Add-SteamVersionSelectionBranchRuntimeChecks {
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

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.cs" `
        "orchestrates selected-version redownload cleanup without touching other branch caches" `
        @(
            "DeleteDownloadedState",
            "SteamGameBranch\.Normalize",
            "GameDirectoryPath\(dataDir, branch\)",
            "SteamGameInstallPaths\.DownloadStateDirectoryPath\(dataDir, branch\)",
            "GameRuntimeSlot\.RuntimePackDirectoryPath\(dataDir, branch\)",
            "WriteRedownloadMarker",
            "DeleteDirectory\(runtimePackDirectory\)",
            "LauncherRuntimeSlotEvidence\.Clear\(dataDir\)",
            "LauncherRuntimeCacheEvidence\.Clear\(dataDir\)",
            "LauncherRuntimePatchValidationEvidence\.Clear\(dataDir\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Fields.cs" `
        "centralizes selected-version redownload marker prefixes" `
        @(
            "RedownloadMarkerUtcPrefix = ""UTC:""",
            "RedownloadMarkerSelectedBranchPrefix = ""Selected branch:""",
            "RedownloadMarkerVersionSlotKindPrefix = ""Selected version slot kind:""",
            "RedownloadMarkerVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
            "RedownloadMarkerGameDirectoryPrefix = ""Deleted game directory:""",
            "RedownloadMarkerGameDirectoryExistedPrefix = ""Game directory existed before delete:""",
            "RedownloadMarkerGameDirectoryExistsAfterDeletePrefix = ""Game directory exists after delete:""",
            "RedownloadMarkerDownloadStateDirectoryPrefix = ""Deleted download state directory:""",
            "RedownloadMarkerDownloadStateDirectoryExistedPrefix = ""Download state directory existed before delete:""",
            "RedownloadMarkerDownloadStateDirectoryExistsAfterDeletePrefix = ""Download state directory exists after delete:""",
            "RedownloadMarkerRuntimePackDirectoryPrefix = ""Deleted runtime pack directory:""",
            "RedownloadMarkerRuntimePackDirectoryExistedPrefix = ""Runtime pack directory existed before delete:""",
            "RedownloadMarkerRuntimePackDirectoryExistsAfterDeletePrefix = ""Runtime pack directory exists after delete:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Read.cs" `
        "reads selected-version redownload cleanup marker evidence" `
        @(
            "RedownloadMarkerFileName",
            "RedownloadMarkerUtcParseable",
            "RedownloadMarkerVersionSlotKind",
            "RedownloadMarkerVersionSlotDirectory",
            "last_game_version_redownload\.txt",
            "RedownloadMarkerRuntimePackDirectory",
            "RedownloadMarkerSelectedDirectoriesCleared"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.Redownload.Marker.Write.cs" `
        "writes selected-version redownload cleanup marker evidence" `
        @(
            "WriteRedownloadMarker",
            "RedownloadMarkerSelectedBranchPrefix",
            "RedownloadMarkerVersionSlotKindPrefix",
            "RedownloadMarkerVersionSlotDirectoryPrefix",
            "RedownloadMarkerGameDirectoryPrefix",
            "RedownloadMarkerGameDirectoryExistsAfterDeletePrefix",
            "RedownloadMarkerDownloadStateDirectoryPrefix",
            "RedownloadMarkerDownloadStateDirectoryExistsAfterDeletePrefix",
            "RedownloadMarkerRuntimePackDirectoryPrefix",
            "RedownloadMarkerRuntimePackDirectoryExistsAfterDeletePrefix",
            "File\.WriteAllLines",
            "Failed to write game version redownload marker"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.cs" `
        "orchestrates inactive cache cleanup while preserving selected non-public cache state" `
        @(
            "DeleteInactiveVersionCaches",
            "NewCacheCleanupMarkerLines",
            "DeleteInactiveRuntimePacks",
            "WriteCacheCleanupMarker",
            "CacheCleanupMarkerRemovedCountPrefix",
            "CacheCleanupMarkerRemovedRuntimePackCountPrefix",
            "Removing inactive game version cache",
            "Preserving selected game version cache",
            "CacheCleanupMarkerPreservedSelectedCachePrefix",
            "selected branch"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.Marker.Fields.cs" `
        "centralizes selected-version cache cleanup marker prefixes" `
        @(
            "CacheCleanupMarkerUtcPrefix = ""UTC:""",
            "CacheCleanupMarkerSelectedBranchPrefix = ""Selected branch:""",
            "CacheCleanupMarkerSelectedVersionPrefix = ""Selected version:""",
            "CacheCleanupMarkerVersionSlotKindPrefix = ""Selected version slot kind:""",
            "CacheCleanupMarkerVersionSlotDirectoryPrefix = ""Selected version slot directory:""",
            "CacheCleanupMarkerGameVersionsDirectoryPresentPrefix = ""Game versions directory present:""",
            "CacheCleanupMarkerRuntimePacksDirectoryPresentPrefix = ""Runtime packs directory present:""",
            "CacheCleanupMarkerSelectedRuntimePackDirectoryPrefix = ""Selected runtime pack directory:""",
            "CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanupPrefix = ""Selected runtime pack present before cleanup:""",
            "CacheCleanupMarkerRemovedCountPrefix = ""Removed count:""",
            "CacheCleanupMarkerRemovedRuntimePackCountPrefix = ""Removed runtime pack count:""",
            "CacheCleanupMarkerRemovedCachePrefix = ""Removed cache:""",
            "CacheCleanupMarkerRemovedRuntimePackPrefix = ""Removed runtime pack:""",
            "CacheCleanupMarkerPreservedSelectedCachePrefix = ""Preserved selected cache:""",
            "CacheCleanupMarkerPreservedSelectedRuntimePackPrefix = ""Preserved selected runtime pack:""",
            "CacheCleanupMarkerRemovedOrphanRuntimePackPrefix = ""Removed orphan runtime pack:"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.Markers.cs" `
        "records and reads cache cleanup marker evidence for selected cache/runtime pack preservation" `
        @(
            "CacheCleanupMarkerFileName",
            "CacheCleanupMarkerPath",
            "CacheCleanupMarkerUtc",
            "CacheCleanupMarkerUtcParseable",
            "CacheCleanupMarkerSelectedBranch",
            "CacheCleanupMarkerSelectedVersion",
            "CacheCleanupMarkerVersionSlotKind",
            "CacheCleanupMarkerVersionSlotDirectory",
            "CacheCleanupMarkerGameVersionsDirectoryPresent",
            "CacheCleanupMarkerRuntimePacksDirectoryPresent",
            "CacheCleanupMarkerSelectedRuntimePackDirectory",
            "CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanup",
            "CacheCleanupMarkerRemovedCount",
            "CacheCleanupMarkerRemovedRuntimePackCount",
            "CacheCleanupMarkerSelectedCachePreservedWhereApplicable",
            "CacheCleanupMarkerSelectedRuntimePackPreservedWhereApplicable",
            "last_game_version_cache_cleanup\.txt",
            "CacheCleanupMarkerSelectedRuntimePackDirectoryPrefix",
            "CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanupPrefix",
            "CacheCleanupMarkerRuntimePacksDirectoryPresentPrefix"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameFiles.CacheCleanup.RuntimePacks.cs" `
        "removes inactive runtime-pack caches while preserving the selected runtime pack" `
        @(
            "RuntimePackDirectoryPathForStateDirectory",
            "DeleteInactiveRuntimePacks",
            "runtime_packs",
            "CacheCleanupMarkerPreservedSelectedRuntimePackPrefix",
            "CacheCleanupMarkerRemovedOrphanRuntimePackPrefix",
            "existsAfterDelete"
        )
}
