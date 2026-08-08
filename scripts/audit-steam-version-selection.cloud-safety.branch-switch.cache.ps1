function Add-SteamVersionSelectionCloudSafetyBranchSwitchCacheChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherGameVersionCache.cs" `
        "enumerates side-by-side cached non-public versions" `
        @(
            "CachedVersion",
            "LauncherStorageNames\.GameVersionsDirectory",
            "Selected"
        )
}
