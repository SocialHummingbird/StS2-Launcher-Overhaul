function Add-SteamVersionSelectionAuthCloudSafetyShellBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.ps1" `
        "keeps branch-switch and manual cloud Push safety audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyChecks",
            "Add-SteamVersionSelectionCloudSafetyBranchSwitchChecks",
            "Add-SteamVersionSelectionCloudSafetyPushRequestChecks",
            "Add-SteamVersionSelectionCloudSafetyEvidenceMarkerChecks",
            "Add-SteamVersionSelectionCloudSafetyLocalBackupChecks",
            "Add-SteamVersionSelectionCloudSafetyStartupContextChecks",
            "audit-steam-version-selection.cloud-safety.branch-switch.ps1",
            "audit-steam-version-selection.cloud-safety.push-requests.ps1",
            "audit-steam-version-selection.cloud-safety.evidence-markers.ps1",
            "audit-steam-version-selection.cloud-safety.local-backups.ps1",
            "audit-steam-version-selection.cloud-safety.startup-context.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.branch-switch.ps1" `
        "keeps branch-switch cache, marker, and gate audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyBranchSwitchChecks",
            "audit-steam-version-selection.cloud-safety.branch-switch.cache.ps1",
            "audit-steam-version-selection.cloud-safety.branch-switch.marker.ps1",
            "audit-steam-version-selection.cloud-safety.branch-switch.gates.ps1",
            "Add-SteamVersionSelectionCloudSafetyBranchSwitchCacheChecks",
            "Add-SteamVersionSelectionCloudSafetyBranchSwitchMarkerChecks",
            "Add-SteamVersionSelectionCloudSafetyBranchSwitchGateChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.branch-switch.cache.ps1" `
        "keeps branch-switch cached-version enumeration checks focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyBranchSwitchCacheChecks",
            "LauncherGameVersionCache.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.branch-switch.marker.ps1" `
        "keeps branch-switch marker identity, fields, and read checks focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyBranchSwitchMarkerChecks",
            "LauncherBranchSwitchSafety.cs",
            "LauncherBranchSwitchSafety.Fields.cs",
            "LauncherBranchSwitchSafety.Read.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.branch-switch.gates.ps1" `
        "keeps branch-switch Push gates and marker write checks focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyBranchSwitchGateChecks",
            "LauncherBranchSwitchSafety.Gates.cs",
            "LauncherBranchSwitchSafety.Write.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.push-requests.ps1" `
        "keeps manual cloud Push gate and request audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyPushRequestChecks",
            "audit-steam-version-selection.cloud-safety.push-requests.gates.ps1",
            "audit-steam-version-selection.cloud-safety.push-requests.request.ps1",
            "audit-steam-version-selection.cloud-safety.push-requests.execution.ps1",
            "Add-SteamVersionSelectionCloudSafetyPushGateChecks",
            "Add-SteamVersionSelectionCloudSafetyPushRequestConstructionChecks",
            "Add-SteamVersionSelectionCloudSafetyPushExecutionChecks"
        )
}
