function Add-SteamVersionSelectionPortalActionPortalUxFlagBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-flags.ps1" `
        "keeps portal UX support flag audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionPortalUxFlagChecks",
            "audit-steam-version-selection.portal-ux-flags.status.ps1",
            "audit-steam-version-selection.portal-ux-flags.workflow.ps1",
            "audit-steam-version-selection.portal-ux-flags.auth-chrome.ps1",
            "audit-steam-version-selection.portal-ux-flags.install-cloud.ps1",
            "audit-steam-version-selection.portal-ux-flags.diagnostics.ps1",
            "Add-SteamVersionSelectionPortalUxStatusFlagChecks",
            "Add-SteamVersionSelectionPortalUxWorkflowFlagChecks",
            "Add-SteamVersionSelectionPortalUxAuthChromeFlagChecks",
            "Add-SteamVersionSelectionPortalUxInstallCloudFlagChecks",
            "Add-SteamVersionSelectionPortalUxDiagnosticsFlagChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-flags.status.ps1" `
        "keeps portal UX status and compact layout support flag contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxStatusFlagChecks",
            "LauncherPortalUxSupport.Status.cs",
            "StatusLedPortalSupported",
            "CompactTouchSafeStatusDetailButtonSupported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-flags.workflow.ps1" `
        "keeps portal UX workflow and sticky task-header support flag contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxWorkflowFlagChecks",
            "LauncherPortalUxSupport.Workflow.cs",
            "CompactWorkflowStepStripSupported",
            "ViewportAwareCompactTaskReanchorSupported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-flags.auth-chrome.ps1" `
        "keeps portal UX auth, confirmation, branding, and section chrome support flag contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxAuthChromeFlagChecks",
            "LauncherPortalUxSupport.AuthChrome.cs",
            "CompactSteamGuardLargeInputSupported",
            "CompactReadableBrandSubtitleSupported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-flags.install-cloud.ps1" `
        "keeps portal UX install, ready-state, cloud, drawer, and scroll support flag contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxInstallCloudFlagChecks",
            "LauncherPortalUxSupport.InstallCloud.cs",
            "CompactInstallPrimaryActionFirstSupported",
            "SaferPullBeforePushOrderingSupported",
            "KeyboardFocusedInputScrollSupported"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.portal-ux-flags.diagnostics.ps1" `
        "keeps portal UX diagnostics and recovery support flag contracts focused" `
        @(
            "function Add-SteamVersionSelectionPortalUxDiagnosticsFlagChecks",
            "LauncherPortalUxSupport.Diagnostics.cs"
        )
}
