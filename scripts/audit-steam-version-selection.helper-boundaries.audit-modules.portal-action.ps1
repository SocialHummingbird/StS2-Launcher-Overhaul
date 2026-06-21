function Add-SteamVersionSelectionPortalActionAuditModuleBoundaryChecks {
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
        "keeps portal status formatter and UX-support audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionPortalUxSupportChecks",
            "Add-SteamVersionSelectionPortalUxStatusFormatterChecks",
            "Add-SteamVersionSelectionPortalUxFlagChecks",
            "Add-SteamVersionSelectionPortalUxNarrativeChecks",
            "Add-SteamVersionSelectionPortalUxFeatureChecks",
            "audit-steam-version-selection.portal-ux-status.ps1",
            "audit-steam-version-selection.portal-ux-flags.ps1",
            "audit-steam-version-selection.portal-ux-narrative.ps1",
            "audit-steam-version-selection.portal-ux-features.ps1"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-status.ps1" `
        "keeps portal status formatter audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxStatusFormatterChecks",
            "LauncherPortalStatusFormatter.cs",
            "LauncherPortalStatusFormatter.Message.cs",
            "LauncherPortalStatusFormatter.Action.cs",
            "LauncherPortalStatusFormatter.Phase.cs",
            "LauncherPortalStatusFormatter.Color.cs",
            "LauncherPortalStatusFormatter.Predicates.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-flags.ps1" `
        "keeps portal UX support flag audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxFlagChecks",
            "LauncherPortalUxSupport.Status.cs",
            "LauncherPortalUxSupport.InstallCloud.cs",
            "LauncherPortalUxSupport.Workflow.cs",
            "LauncherPortalUxSupport.AuthChrome.cs",
            "LauncherPortalUxSupport.Diagnostics.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-narrative.ps1" `
        "keeps portal UX narrative audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxNarrativeChecks",
            "LauncherPortalUxSupport.cs",
            "Status-led launcher portal",
            "compact ready-state priority",
            "compact Play/Sync uses plain-language save copy",
            "ARM64 visual validation"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-features.ps1" `
        "keeps portal UX diagnostics feature audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxFeatureChecks",
            "LauncherPortalUxSupport.Features.cs",
            "LauncherPortalUxSupport.Features.Status.cs",
            "LauncherPortalUxSupport.Features.Workflow.cs",
            "LauncherPortalUxSupport.Features.AuthChrome.cs",
            "LauncherPortalUxSupport.Features.InstallCloud.cs",
            "LauncherPortalUxSupport.Features.Diagnostics.cs",
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
