function Add-SteamVersionSelectionAuditModuleBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.launcher-shell.ps1" `
        "keeps launcher shell audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLauncherShellChecks",
            "LauncherUI.cs",
            "LauncherUI.Lifecycle.cs",
            "LauncherUI.MainThread.cs",
            "LauncherUI.Viewport.cs",
            "LauncherUI.AutoLaunch.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-selector.ps1" `
        "keeps Steam branch selector audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchSelectorChecks",
            "SteamGameBranch.cs",
            "LauncherBranchCatalog.Option.cs",
            "LauncherBranchDropdown.cs",
            "DownloadSection.Branches.cs",
            "ActionSection.Branches.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-runtime.ps1" `
        "keeps Steam branch runtime and cache audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchRuntimeChecks",
            "DepotDownloader.DepotManifestReference.cs",
            "LauncherBranchMarkerFields.cs",
            "LauncherAndroidAppPrivatePath.cs",
            "LauncherGameFiles.Redownload.cs",
            "LauncherGameFiles.CacheCleanup.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.native-routing.ps1" `
        "keeps native selected-branch routing and fallback audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionNativeRoutingChecks",
            "GodotApp.java",
            "SteamBranchInfo.java",
            "LauncherActivity.java",
            "NativeFallbackActivity.java",
            "requires selected branch provenance before consuming native game launch requests"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.download-workflows.ps1" `
        "keeps download, update, and branch-refresh workflow audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionDownloadWorkflowChecks",
            "LauncherModel.Downloads.cs",
            "LauncherModel.Downloads.Action.cs",
            "LauncherController.Downloads.Actions.cs",
            "LauncherController.UpdateChecks.cs",
            "LauncherController.UpdateChecks.ViewUpdate.cs",
            "LauncherController.UpdateChecks.Run.cs",
            "LauncherController.UpdateChecks.Workflow.cs",
            "LauncherController.UpdateChecks.Results.cs"
        )

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
        "keeps branch-switch and manual cloud Push safety audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCloudSafetyChecks",
            "LauncherBranchSwitchSafety.cs",
            "LauncherController.CloudSync.PushSafety.cs",
            "LauncherCloudSyncEvidence.Fields.cs",
            "LauncherLocalSaveEvidence.cs",
            "LauncherBackupEvidence.cs",
            "CloudSyncCoordinator.SaveBackups.Manual.cs",
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

    Add-Check `
        "scripts\audit-steam-version-selection.compact-labels.ps1" `
        "keeps reusable compact two-line label audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactLabelChecks",
            "CompactButtonDetailLabelSpec.cs",
            "CompactButtonDetailLabels.cs",
            "CompactButtonDetailLabels.Text.cs",
            "CompactButtonDetailLabels.Controls.cs",
            "LoginSection.CompactNativeButton.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.section-setup.ps1" `
        "keeps compact section setup and cue-text audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionSectionSetupChecks",
            "LauncherSectionSetup.cs",
            "LauncherSectionSetup.Header.cs",
            "LauncherSectionSetup.Header.Compact.cs",
            "LoginSection.cs",
            "CodeSection.cs",
            "DownloadSection.cs",
            "ActionSection.Construction.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.safe-flow-guide.ps1" `
        "keeps quick-start safe-flow guide audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionSafeFlowGuideChecks",
            "LauncherView.Layout.FirstRunGuide.cs",
            "LauncherView.Layout.FirstRunGuide.Steps.cs",
            "LauncherView.Layout.FirstRunGuide.StepCard.cs",
            "LauncherView.Layout.FirstRunGuide.Toggle.cs",
            "CompactSafeFlowStepSpec"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-drawer.ps1" `
        "keeps Help & Reports diagnostics drawer audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsDrawerChecks",
            "LauncherView.Layout.LogColumn.cs",
            "LauncherView.Layout.LogColumn.Toggle.cs",
            "LauncherView.Layout.LogColumn.Sizing.cs",
            "LauncherView.Diagnostics.cs",
            "LauncherController.Diagnostics.Export.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.diagnostics-reporting.ps1" `
        "keeps launcher diagnostics report and branch-switch evidence audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionDiagnosticsReportingChecks",
            "LauncherController.Diagnostics.cs",
            "LauncherDiagnostics.Reports.cs",
            "LauncherDiagnostics.ReportLauncherPreferences.cs",
            "LauncherDiagnostics.ReportBranchAvailability.cs",
            "LauncherDiagnostics.ReportCachedGameVersions.cs",
            "LauncherDiagnostics.ReportBranchSwitchSafety.cs",
            "LauncherDiagnostics.ReportBranchSwitchSafety.Push.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.evidence-tooling.ps1" `
        "keeps Steam version-selection evidence tooling audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionEvidenceToolingChecks",
            "android-adb-utils.ps1",
            "capture-steam-version-selection-evidence.ps1",
            "new-steam-version-selection-evidence.ps1",
            "export-public-evidence-redaction.ps1",
            "review-public-evidence-redaction.ps1",
            "audit-steam-branch-guidance-parity.ps1",
            "steam-version-selection-tooling.md"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.release-docs.ps1" `
        "keeps Steam version-selection release/readiness documentation audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionReleaseDocsChecks",
            "steam-version-selection-validation.md",
            "steam-version-selection-release-readiness.md",
            "steam-version-selection-architecture.md",
            "steam-version-selection-completion-audit.md",
            "steam-version-selection-runbook.md",
            "steam-version-selection-user-guide.md",
            "android-release-validation.md"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.beta-integrity.ps1" `
        "keeps beta branch integrity evidence audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBetaIntegrityChecks",
            "capture-steam-beta-integrity-evidence.ps1",
            "review-beta-integrity-summary.ps1",
            "steam-beta-integrity-runtime-checklist.md",
            "Public-vs-beta branch integrity",
            "Public-vs-beta depot manifest integrity",
            "Public-vs-beta file inventory"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-chrome.ps1" `
        "keeps launcher shell, brand, and compact panel chrome audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionPortalChromeChecks",
            "LauncherView.Layout.cs",
            "LauncherView.Layout.BrandHeader.cs",
            "LauncherView.Layout.BrandMark.cs",
            "LauncherLayoutProfile.cs",
            "StyledPanel.cs",
            "LauncherView.Layout.PrimaryColumn.Support.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.status-capsule.ps1" `
        "keeps launcher status capsule audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionStatusCapsuleChecks",
            "LauncherView.Layout.StatusCapsule.cs",
            "LauncherView.Layout.StatusCapsule.Compact.cs",
            "LauncherView.Layout.StatusCapsule.Detail.cs",
            "LauncherView.Layout.StatusCapsule.Styles.cs",
            "LauncherView.Status.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-workflow.ps1" `
        "keeps compact workflow and current-task audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactWorkflowChecks",
            "LauncherView.Layout.CompactWorkflow.cs",
            "LauncherView.Layout.CompactTaskHeader.cs",
            "LauncherView.Layout.PrimaryColumn.Body.cs",
            "LauncherView.CompactWorkflow.Data.cs",
            "LauncherView.CompactWorkflow.State.cs",
            "LauncherView.CompactWorkflow.Navigation.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.code-section.ps1" `
        "keeps compact Steam Guard code-section audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCodeSectionChecks",
            "CodeSection.cs",
            "CodeSection.Labels.cs",
            "CodeSection.Input.cs",
            "CodeSection.SubmitButton.cs",
            "CodeSection.Layout.cs",
            "CodeSection.Prompt.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-section-flow.ps1" `
        "keeps compact section visibility and scroll-flow audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactSectionFlowChecks",
            "SetCompactReadyInstallSectionVisible",
            "HideCompactCompletedAuthSections",
            "ScrollCompactPrimaryTo",
            "CompactScrollAnchorTopPadding",
            "ReanchorCompactScrollTargetAfterViewportChange",
            "ReadyScrollTarget"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.compact-install.ps1" `
        "keeps compact install/version/download audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionCompactInstallChecks",
            "DownloadSection.cs",
            "DownloadSection.Construction.Version.cs",
            "DownloadSection.CompactVersion.cs",
            "DownloadSection.CompactDownload.cs",
            "DownloadSection.Progress.cs",
            "LauncherComponentTheme.cs",
            "new StyledProgressBar"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.ps1" `
        "keeps startup safe-mode, shader warmup, and startup-status audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupChecks",
            "LauncherStartupFlow.StartupMode.cs",
            "ShaderWarmupScreen.cs",
            "StyledProgressBar.cs",
            "LauncherStartupStatus.cs",
            "LauncherStartupStatus.Android.cs",
            "LauncherGameStartupRecovery.State.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-recovery.ps1" `
        "keeps startup recovery panel and report audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionStartupRecoveryChecks",
            "LauncherDiagnostics.StartupRecoveryReports.cs",
            "LauncherStartupRecoveryControlPanel.Text.cs",
            "LauncherStartupRecoveryControlPanel.Buttons.cs",
            "LauncherStartupRecoveryControlPanel.Layout.cs",
            "LauncherStartupRecoveryControlPanel.Construction.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-section.ps1" `
        "keeps ready-state action audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionActionSectionChecks",
            "Add-SteamVersionSelectionActionCoreChecks",
            "Add-SteamVersionSelectionActionSupportChecks",
            "Add-SteamVersionSelectionActionCloudControlsChecks",
            "Add-SteamVersionSelectionActionVisibilityChecks",
            "Add-SteamVersionSelectionActionCloudSafetyChecks",
            "Add-SteamVersionSelectionActionSupportDiagnosticsChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-core.ps1" `
        "keeps ready-state action core and button-style audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCoreChecks",
            "ActionSection.Construction.cs",
            "ActionSection.Construction.Ready.cs",
            "ActionSection.ReadySummary.cs",
            "ActionSection.Layout.cs",
            "LauncherButtonStyles.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-support.ps1" `
        "keeps ready-state support-tool audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionSupportChecks",
            "function Add-SteamVersionSelectionActionSupportDiagnosticsChecks",
            "ActionSection.Construction.Support.cs",
            "ActionSection.Construction.Support.Tools.cs",
            "ActionSection.Construction.Support.DiagnosticsTools.cs",
            "ActionSection.Support.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-cloud.ps1" `
        "keeps ready-state cloud-control audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionCloudControlsChecks",
            "function Add-SteamVersionSelectionActionCloudSafetyChecks",
            "ActionSection.Construction.Cloud.cs",
            "ActionSection.Construction.Cloud.PrimaryActions.cs",
            "ActionSection.Construction.Cloud.PushConfirmation.cs",
            "ActionSection.CloudSafety.cs",
            "ActionSection.CloudPush.cs",
            "ActionSection.Visibility.Cloud.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.action-visibility.ps1" `
        "keeps ready-state launch and visibility audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionActionVisibilityChecks",
            "ActionSection.Visibility.cs",
            "ActionSection.Visibility.SecondaryState.cs",
            "ActionSection.Visibility.Secondary.cs",
            "ActionSection.Visibility.Support.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-support.ps1" `
        "keeps portal status formatter and UX-support audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionPortalUxSupportChecks",
            "LauncherPortalStatusFormatter.cs",
            "LauncherPortalStatusFormatter.Message.cs",
            "LauncherPortalUxSupport.Status.cs",
            "LauncherPortalUxSupport.InstallCloud.cs",
            "LauncherPortalUxSupport.Features.cs",
            "PortalUxDeviceValidated"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.branch-availability.ps1" `
        "keeps Steam branch availability audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionBranchAvailabilityChecks",
            "SteamBranchAvailabilityMarkerFields",
            "SteamBranchAvailabilityMarkerFile",
            "SteamBranchAvailabilityMarkerRow",
            "LauncherBranchAvailabilityStatus.Read.cs",
            "DepotDownloader.BranchAvailability.Marker.cs"
        )
}
