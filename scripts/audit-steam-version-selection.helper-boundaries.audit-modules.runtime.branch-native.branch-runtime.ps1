function Add-SteamVersionSelectionRuntimeBranchBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.ps1" `
        "keeps Steam branch runtime and cache audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeChecks",
            "audit-steam-version-selection.branch-runtime.depots.ps1",
            "audit-steam-version-selection.branch-runtime.markers.ps1",
            "audit-steam-version-selection.branch-runtime.cache-safety.ps1",
            "Add-SteamVersionSelectionBranchRuntimeDepotChecks",
            "Add-SteamVersionSelectionBranchRuntimeMarkerChecks",
            "Add-SteamVersionSelectionBranchRuntimeCacheSafetyChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.depots.ps1" `
        "keeps Steam depot provenance and inherited-public manifest audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeDepotChecks",
            "DepotDownloader.cs",
            "DepotDownloader.Depots.cs",
            "DepotDownloader.DepotDownload.cs",
            "DepotDownloader.DepotManifestReference.cs",
            "public-inherited",
            "ManifestRequestBranch"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.markers.ps1" `
        "keeps branch marker readiness and integrity audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeMarkerChecks",
            "audit-steam-version-selection.branch-runtime.markers.diagnostics.ps1",
            "audit-steam-version-selection.branch-runtime.markers.fields.ps1",
            "audit-steam-version-selection.branch-runtime.markers.readiness.ps1",
            "Add-SteamVersionSelectionBranchRuntimeMarkerDiagnosticsChecks",
            "Add-SteamVersionSelectionBranchRuntimeMarkerFieldChecks",
            "Add-SteamVersionSelectionBranchRuntimeMarkerReadinessChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.markers.diagnostics.ps1" `
        "keeps branch marker diagnostics audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeMarkerDiagnosticsChecks",
            "LauncherDiagnostics.ReportLauncherPreferences.cs",
            "LauncherDiagnostics.ReportBranchMarkers.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.markers.fields.ps1" `
        "keeps branch marker typed field and path audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeMarkerFieldChecks",
            "LauncherBranchMarkerFields.cs",
            "LauncherBranchMarkerIntegrityProvenance.cs",
            "LauncherAndroidAppPrivatePath.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.markers.readiness.ps1" `
        "keeps branch marker launch-readiness audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeMarkerReadinessChecks",
            "LauncherGameFiles.BranchMarker.cs",
            "LauncherGameFiles.BranchIntegrity.cs",
            "LauncherGameFiles.Readiness.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.cache-safety.ps1" `
        "keeps selected-version redownload and inactive-cache cleanup audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeCacheSafetyChecks",
            "audit-steam-version-selection.branch-runtime.cache-safety.redownload.ps1",
            "audit-steam-version-selection.branch-runtime.cache-safety.cleanup-markers.ps1",
            "audit-steam-version-selection.branch-runtime.cache-safety.runtime-packs.ps1",
            "Add-SteamVersionSelectionBranchRuntimeRedownloadCleanupChecks",
            "Add-SteamVersionSelectionBranchRuntimeCacheCleanupMarkerChecks",
            "Add-SteamVersionSelectionBranchRuntimePackCleanupChecks"
        )
}
