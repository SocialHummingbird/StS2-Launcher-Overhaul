function Add-SteamVersionSelectionAuthCloudAuditModuleBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.session-auth.ps1" `
        "keeps Steam session authentication audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionSessionAuthChecks",
            "LauncherModel.SessionAuth.cs",
            "LauncherModel.SessionAuth.Attempt.cs",
            "LauncherSteamSession.Connection.SavedCredentials.cs",
            "LauncherSteamSession.Connection.Ensure.cs",
            "LauncherSteamSession.Connection.Adoption.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.automation.ps1" `
        "keeps launcher automation audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionAutomationChecks",
            "LauncherController.Automation.cs",
            "LauncherController.Automation.Request.cs",
            "LauncherController.Automation.Run.cs",
            "LauncherController.Automation.Marker.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.local-login.ps1" `
        "keeps local Steam credential handoff audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLocalLoginChecks",
            "LauncherController.LocalLogin.cs",
            "LauncherController.LocalLogin.Start.cs",
            "LauncherController.LocalLogin.Handoff.cs",
            "LauncherController.LocalLogin.Watch.cs",
            "LauncherController.LocalLogin.Run.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.confirmations.ps1" `
        "keeps confirmation dialog and contextual-action audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionConfirmationChecks",
            "LauncherView.Dialog.Buttons.cs",
            "LauncherView.Dialog.Message.cs",
            "LauncherView.Behavior.Confirmation.cs",
            "LauncherController.Startup.BranchSwitch.cs",
            "LauncherController.CloudSync.Request.Factory.cs"
        )

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
        "keeps branch-switch cache and marker audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyBranchSwitchChecks",
            "LauncherGameVersionCache.cs",
            "LauncherBranchSwitchSafety.cs",
            "LauncherBranchSwitchSafety.Fields.cs",
            "LauncherBranchSwitchSafety.Gates.cs",
            "LauncherBranchSwitchSafety.Write.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.push-requests.ps1" `
        "keeps manual cloud Push gate and request audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyPushRequestChecks",
            "LauncherController.CloudSync.PushSafety.cs",
            "LauncherController.CloudSync.PushSafety.Context.cs",
            "LauncherController.CloudSync.PushSafety.Baseline.cs",
            "LauncherController.CloudSync.PushSafety.BranchSwitch.cs",
            "LauncherController.CloudSync.Request.cs",
            "LauncherController.CloudSync.Request.Factory.cs",
            "LauncherController.CloudSync.Request.PushConfirmation.cs",
            "LauncherController.CloudSync.Request.Lifecycle.cs",
            "LauncherController.CloudSync.Execution.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.evidence-markers.ps1" `
        "keeps manual cloud evidence marker audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyEvidenceMarkerChecks",
            "LauncherCloudSyncEvidence.cs",
            "LauncherCloudSyncEvidence.Fields.cs",
            "LauncherCloudSyncEvidence.Pull.cs",
            "LauncherCloudSyncEvidence.Push.Latest.cs",
            "LauncherCloudSyncEvidence.BlockedPush.cs",
            "LauncherCloudSyncEvidence.Markers.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.local-backups.ps1" `
        "keeps local-save and pre-Push backup evidence audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyLocalBackupChecks",
            "LauncherLocalSaveEvidence.cs",
            "LauncherLocalSaveEvidence.Classify.cs",
            "LauncherLocalSaveEvidence.Enumeration.cs",
            "LauncherBackupEvidence.cs",
            "LauncherBackupEvidence.BranchSwitch.cs",
            "CloudSyncCoordinator.SaveBackups.Manual.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.cloud-safety.startup-context.ps1" `
        "keeps branch-switch startup context audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyStartupContextChecks",
            "LauncherController.Startup.BranchSwitch.cs",
            "LauncherController.Startup.RuntimeEvidence.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.login-panel.ps1" `
        "keeps native credential panel and login-section audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLoginPanelChecks",
            "LauncherCredentialEntrySupport.cs",
            "LoginSection.Submission.cs",
            "LoginSection.NativePanel.cs",
            "LoginSection.Help.cs",
            "LoginSection.State.cs"
        )
}
