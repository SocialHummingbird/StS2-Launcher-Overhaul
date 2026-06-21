function Add-SteamVersionSelectionPortalActionReadyActionCloudSafetyBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.safety.ps1" `
        "keeps ready-state Steam Cloud safety and visibility audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionActionCloudSafetyChecks",
            "audit-steam-version-selection.action-cloud.safety.push-flow.ps1",
            "audit-steam-version-selection.action-cloud.safety.compact-options.ps1",
            "audit-steam-version-selection.action-cloud.safety.cue.ps1",
            "Add-SteamVersionSelectionActionCloudSafetyPushFlowChecks",
            "Add-SteamVersionSelectionActionCloudSafetyCompactOptionChecks",
            "Add-SteamVersionSelectionActionCloudSafetyCueChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.safety.push-flow.ps1" `
        "keeps ready-state Steam Cloud Push direction and arming contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCloudSafetyPushFlowChecks",
            "ActionSection.Construction.Cloud.PrimaryActions.cs",
            "ActionSection.CloudPush.cs",
            "ActionSection.Branches.Text.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.safety.compact-options.ps1" `
        "keeps ready-state compact Steam Cloud option contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCloudSafetyCompactOptionChecks",
            "ActionSection.Visibility.Cloud.cs",
            "ActionSection.CloudOptions.cs",
            "ActionSection.Toggles.cs",
            "ActionSection.ConstructionHelpers.cs",
            "ActionSection.Construction.Cloud.Options.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.safety.cue.ps1" `
        "keeps ready-state Steam Cloud safety cue contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCloudSafetyCueChecks",
            "ActionSection.Construction.Cloud.PrimaryActions.cs",
            "ActionSection.Construction.Cloud.Safety.cs",
            "ActionSection.Layout.cs"
        )
}
