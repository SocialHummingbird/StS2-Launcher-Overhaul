function Add-SteamVersionSelectionPortalActionReadyActionCloudControlBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.ps1" `
        "keeps ready-state cloud-control audit contracts behind a focused aggregator" `
        @(
            "audit-steam-version-selection.action-cloud.controls.ps1",
            "audit-steam-version-selection.action-cloud.safety.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.controls.ps1" `
        "keeps ready-state cloud-control audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionActionCloudControlsChecks",
            "audit-steam-version-selection.action-cloud.controls.construction.ps1",
            "audit-steam-version-selection.action-cloud.controls.primary-actions.ps1",
            "audit-steam-version-selection.action-cloud.controls.copy.ps1",
            "audit-steam-version-selection.action-cloud.controls.layout.ps1",
            "Add-SteamVersionSelectionActionCloudControlConstructionChecks",
            "Add-SteamVersionSelectionActionCloudControlPrimaryActionChecks",
            "Add-SteamVersionSelectionActionCloudControlCopyChecks",
            "Add-SteamVersionSelectionActionCloudControlLayoutChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.controls.construction.ps1" `
        "keeps ready-state cloud-control construction audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCloudControlConstructionChecks",
            "ActionSection.Construction.Cloud.cs",
            "CloudControls",
            "BuildCloudPrimaryActionControls",
            "BuildCloudSafetyControls",
            "BuildCloudOptionControls"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.controls.primary-actions.ps1" `
        "keeps ready-state cloud primary-action and confirmation audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCloudControlPrimaryActionChecks",
            "ActionSection.Construction.Cloud.PrimaryActions.cs",
            "ActionSection.Construction.Cloud.PushConfirmation.cs",
            "CompactCloudPullText",
            "CompactCloudPushDangerText",
            "CompactCloudPushWarningText"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.controls.copy.ps1" `
        "keeps ready-state compact cloud copy audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCloudControlCopyChecks",
            "ActionSection.CloudSafety.cs",
            "ActionSection.CloudPush.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.controls.layout.ps1" `
        "keeps ready-state compact cloud layout and ordering audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCloudControlLayoutChecks",
            "ActionSection.ConstructionHelpers.cs",
            "ActionSection.Construction.Cloud.PrimaryActions.cs",
            "BuildCompactCloudPrimaryActionsRow",
            "_compactStackedActionRows"
        )
}
