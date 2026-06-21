function Add-SteamVersionSelectionAuthCloudSafetyMarkerBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.evidence-markers.ps1" `
        "keeps manual cloud evidence marker audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerChecks",
            "audit-steam-version-selection.cloud-safety.evidence-markers.core.ps1",
            "audit-steam-version-selection.cloud-safety.evidence-markers.pull.ps1",
            "audit-steam-version-selection.cloud-safety.evidence-markers.push.ps1",
            "audit-steam-version-selection.cloud-safety.evidence-markers.blocked-push.ps1",
            "Add-SteamVersionSelectionCloudSafetyEvidenceMarkerCoreChecks",
            "Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPullChecks",
            "Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPushChecks",
            "Add-SteamVersionSelectionCloudSafetyEvidenceMarkerBlockedPushChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.evidence-markers.core.ps1" `
        "keeps shared manual cloud evidence marker prefix and parser checks focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerCoreChecks",
            "LauncherCloudSyncEvidence.cs",
            "LauncherCloudSyncEvidence.Fields.cs",
            "LauncherCloudSyncEvidence.Markers.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.evidence-markers.pull.ps1" `
        "keeps manual Pull evidence marker checks focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPullChecks",
            "LauncherCloudSyncEvidence.Pull.cs",
            "ManualPullCompletedBeforePushPrefix",
            "ManualPullCompletedBeforeBranchSwitchPushPrefix"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.evidence-markers.push.ps1" `
        "keeps completed manual Push evidence marker checks focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerPushChecks",
            "LauncherCloudSyncEvidence.Push.cs",
            "LauncherCloudSyncEvidence.Push.Latest.cs",
            "LauncherCloudSyncEvidence.Push.Read.cs",
            "LauncherCloudSyncEvidence.Push.Safety.cs",
            "LauncherCloudSyncEvidence.Push.Write.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.evidence-markers.blocked-push.ps1" `
        "keeps blocked manual Push evidence marker checks focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerBlockedPushChecks",
            "LauncherCloudSyncEvidence.BlockedPush.cs",
            "ManualPushBlockedBeforeUploadPrefix",
            "ManualPushBlockedReasonPrefix"
        )
}
