function Add-SteamVersionSelectionBranchRuntimePackCleanupChecks {
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
