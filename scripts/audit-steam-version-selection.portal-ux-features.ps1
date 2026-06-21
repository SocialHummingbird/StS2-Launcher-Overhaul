function Add-SteamVersionSelectionPortalUxFeatureChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.cs" `
        "builds launcher UI hardening diagnostics feature registry from category-owned reports" `
        @(
            "FeatureReports => BuildFeatureReports",
            "new List<LauncherPortalUxFeature>",
            "AddStatusFeatureReports\(features\)",
            "AddWorkflowFeatureReports\(features\)",
            "AddAuthChromeFeatureReports\(features\)",
            "AddInstallCloudFeatureReports\(features\)",
            "AddDiagnosticsFeatureReports\(features\)",
            "return features\.ToArray\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.Status.cs" `
        "keeps status and safe-flow feature diagnostics beside status support flags" `
        @(
            "AddStatusFeatureReports",
            "status-led portal supported",
            "CompactStackedStatusHeaderSupported",
            "ViewportAwareCompactStatusHeadlineReflowSupported",
            "CompactShortStatusDetailsSupported",
            "CompactTouchSafeStatusDetailButtonSupported",
            "CompactStatusDetailCueSupported"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.Workflow.cs" `
        "keeps workflow and current-task feature diagnostics beside workflow support flags" `
        @(
            "AddWorkflowFeatureReports",
            "CompactWorkflowStepStripSupported",
            "CompactWorkflowStepNumberBadgesSupported",
            "CompactCurrentTaskJumpSupported",
            "CompactStickyTaskToolbarShellSupported",
            "ViewportAwareCompactTaskReanchorSupported",
            "CompactReadableDetailLabelFontSupported"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.AuthChrome.cs" `
        "keeps sign-in, action, and chrome feature diagnostics beside auth/chrome support flags" `
        @(
            "AddAuthChromeFeatureReports",
            "CompactSteamGuardLargeInputSupported",
            "ViewportAwareCompactSteamGuardActionRowReflowSupported",
            "CompactAndroidLoginPrimaryCtaSupported",
            "ConsistentStartGameCtaSupported",
            "BrandedAtmosphericBackgroundSupported",
            "CompactSingleRowSectionHeadersSupported",
            "CompactExplicitSectionHeaderCuesSupported"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.InstallCloud.cs" `
        "keeps install, launch, and cloud feature diagnostics beside install/cloud support flags" `
        @(
            "AddInstallCloudFeatureReports",
            "CompactInstallPrimaryActionFirstSupported",
            "CompactSelectedVersionSummarySupported",
            "CompactReadyVersionSummarySupported",
            "CompactPlainLanguagePlaySyncLabelsSupported",
            "CompactCloudPushDangerDetailLabelsSupported",
            "CompactSupportToolsGridSupported",
            "CompactRawLogReviewLabelSupported",
            "ViewportAwareKeyboardOffsetSupported",
            "CompactActiveTaskSafeFlowSuppressionSupported"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Features.Diagnostics.cs" `
        "keeps fallback, diagnostics, and validation feature diagnostics beside diagnostics support flags" `
        @(
            "AddDiagnosticsFeatureReports",
            "NativeFallbackRecoveryActionsStyledSupported",
            "StartupRecoveryScrollSafeControlsSupported",
            "VersionInstallCloudSeparationGuidanceSupported",
            "DiagnosticsConsoleHiddenByDefault",
            "PlainLanguageHelpReportCopySupported",
            "ViewportAwareDiagnosticsLogResizeSupported",
            "PortalUxDeviceValidated"
        )

}
