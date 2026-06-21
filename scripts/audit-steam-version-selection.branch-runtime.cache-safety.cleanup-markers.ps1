function Add-SteamVersionSelectionBranchRuntimeCacheCleanupMarkerChecks {
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
}
