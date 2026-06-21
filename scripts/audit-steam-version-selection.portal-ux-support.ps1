function Add-SteamVersionSelectionPortalUxSupportChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.cs" `
        "declares status formatter UX support flags" `
        @(
            "internal static partial class LauncherPortalStatusFormatter",
            "PhaseLabelStatusSupported\s*=\s*true",
            "StructuredStatusChipSupported\s*=\s*true",
            "GuidedNextActionStatusSupported\s*=\s*true",
            "ErrorFirstGuidedStatusSupported\s*=\s*true",
            "CompactPlainLanguageStatusCopySupported\s*=\s*true",
            "CompactShortStatusDetailsSupported\s*=\s*true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Message.cs" `
        "formats compact launcher status text as plain-language user copy" `
        @(
            "MessageFor",
            "CompactMessageFor",
            "CompactMessageMaxChars = 86",
            "ShortenCompactMessage",
            "Waiting for launcher state",
            "Sign in with Steam to continue",
            "Signing in to Steam",
            "Checking game ownership",
            "Download this game version to play",
            "Ready to play this version",
            "Signed in\. Checking game files",
            "Get Steam saves before uploading",
            "Upload blocked\. Check save safety first",
            "Runtime files need repair\. Redownload this version",
            "Last launch failed\. Open details or try Safe Start"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Action.cs" `
        "formats launcher status next-action labels" `
        @(
            "ActionFor",
            "Fix Required",
            "Verify Code",
            "Install Game",
            "Start Game",
            "Choose Version",
            "Sync Saves",
            "Review Details",
            "Next Step"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Phase.cs" `
        "classifies launcher status into clear phase labels" `
        @(
            "PhaseFor",
            "Attention",
            "Steam",
            "Version",
            "Install",
            "Cloud",
            "Ready",
            "Details",
            "Status"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Color.cs" `
        "maps launcher status phases to portal colors" `
        @(
            "ColorFor",
            "LauncherComponentTheme\.OrangeHot",
            "LauncherComponentTheme\.CyanAccent",
            "LauncherComponentTheme\.OrangeAccent",
            "new Color\(0\.36f, 0\.9f, 0\.42f\)",
            "LauncherComponentTheme\.TextSecondary"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalStatusFormatter.Predicates.cs" `
        "centralizes launcher status text predicates" `
        @(
            "ContainsAny",
            "StringComparison\.OrdinalIgnoreCase",
            "ContainsFailure",
            "Could not",
            "IsDownloadRequiredStatus",
            "IsReadyStatus",
            "Runtime pairing is verified",
            "Active install slot"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Status.cs" `
        "declares status, safe-flow, and compact layout portal UX support flags" `
        @(
            "StatusLedPortalSupported\s*=\s*true",
            "PhaseLabelStatusSupported",
            "StructuredStatusChipSupported",
            "GuidedNextActionStatusSupported",
            "ErrorFirstGuidedStatusSupported",
            "CompactPlainLanguageStatusCopySupported",
            "TitledStateSectionsSupported\s*=\s*true",
            "SafeFirstRunGuidanceSupported\s*=\s*true",
            "CompactSafeFlowCollapsibleSupported\s*=\s*true",
            "CompactLowProfileSafeFlowToggleSupported\s*=\s*true",
            "CompactSafeFlowToggleDetailLabelsSupported\s*=\s*true",
            "CompactStructuredSafeFlowToggleLabelsSupported\s*=\s*true",
            "CompactSafeFlowBoundedGuideSupported\s*=\s*true",
            "CompactPlainLanguageQuickStartLabelsSupported\s*=\s*true",
            "MobileFirstCompactLayoutSupported\s*=\s*true",
            "CompactDensePanelPaddingSupported\s*=\s*true",
            "CompactDenseVerticalRhythmSupported\s*=\s*true",
            "RoundedScaledLauncherMetricsSupported\s*=\s*true",
            "AndroidCompactTouchScaleFloorSupported\s*=\s*true",
            "AndroidReadableWarmupScreenSupported\s*=\s*true",
            "AndroidReadableStartupStatusCardSupported\s*=\s*true",
            "CompactDynamicContentWidthSupported\s*=\s*true",
            "TabletWideContentLayoutSupported\s*=\s*true",
            "PortalTopAnchoredContentSupported\s*=\s*true",
            "CompactVerticalStatusHeroSupported\s*=\s*true",
            "CompactStackedStatusHeaderSupported\s*=\s*true",
            "CompactLowProfileStatusCardSupported\s*=\s*true",
            "CompactStatusHeadlineRowSupported\s*=\s*true",
            "CompactStackedStatusHeadlineSupported\s*=\s*true",
            "ViewportAwareCompactStatusHeadlineReflowSupported\s*=\s*true",
            "CompactStableStatusDetailRowSupported\s*=\s*true",
            "CompactShortStatusDetailsSupported",
            "CompactStatusTapToExpandDetailsSupported\s*=\s*true",
            "CompactTouchSafeStatusDetailButtonSupported\s*=\s*true",
            "CompactStatusDetailCueSupported\s*=\s*true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Workflow.cs" `
        "declares compact workflow and sticky task-header portal UX support flags" `
        @(
            "CompactWorkflowStepStripSupported\s*=\s*true",
            "CompactTwoColumnWorkflowStripSupported\s*=\s*false",
            "CompactSingleRowNumberedWorkflowStripSupported\s*=\s*true",
            "CompactNarrowWorkflowSingleRowSupported\s*=\s*true",
            "CompactVisibleWorkflowStepLabelsSupported\s*=\s*true",
            "CompactWorkflowStepDetailLabelsSupported\s*=\s*true",
            "CompactWorkflowStepNumberBadgesSupported\s*=\s*true",
            "CompactReadableWorkflowStepNumberBadgesSupported\s*=\s*true",
            "CompactWorkflowUnifiedTouchHeightSupported\s*=\s*true",
            "CompactStickyWorkflowStepStripSupported\s*=\s*true",
            "CompactLowProfileWorkflowStepStripSupported\s*=\s*true",
            "CompactLowProfileTwoColumnWorkflowStepStripSupported\s*=\s*false",
            "CompactWorkflowStepDirectNavigationSupported\s*=\s*true",
            "CompactCurrentTaskJumpSupported\s*=\s*true",
            "CompactStickyCurrentTaskBarSupported\s*=\s*true",
            "CompactLowProfileCurrentTaskBarSupported\s*=\s*true",
            "CompactDenseInlineCurrentTaskBarSupported\s*=\s*true",
            "CompactCurrentTaskSharedTouchHeightSupported\s*=\s*true",
            "CompactLowProfileStackedCurrentTaskBarSupported\s*=\s*true",
            "CompactCurrentTaskContextLabelsSupported\s*=\s*true",
            "CompactStructuredCurrentTaskLabelsSupported\s*=\s*true",
            "CompactCurrentTaskShortTitleLabelsSupported\s*=\s*true",
            "CompactTouchSafeStickyHeaderControlsSupported\s*=\s*true",
            "CompactGroupedStickyTaskHeaderSupported\s*=\s*true",
            "CompactStickyTaskToolbarShellSupported\s*=\s*true",
            "CompactInlineStickyTaskHeaderSupported\s*=\s*true",
            "CompactResponsiveStickyTaskHeaderSupported\s*=\s*true",
            "ViewportAwareStickyTaskHeaderReflowSupported\s*=\s*true",
            "ViewportAwareCompactTaskReanchorSupported\s*=\s*true",
            "CompactDenseStickyTaskHeaderSupported\s*=\s*true",
            "CompactTaskJumpNavigationLabelsSupported\s*=\s*true",
            "CompactReadableDetailLabelFontSupported\s*=\s*true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.AuthChrome.cs" `
        "declares compact auth, confirmation, branding, and section chrome support flags" `
        @(
            "CompactTouchSafeConfirmationDialogsSupported\s*=\s*true",
            "CompactScrollSafeConfirmationDialogsSupported\s*=\s*true",
            "CompactContextualConfirmationLabelsSupported\s*=\s*true",
            "ViewportAwareConfirmationDialogsSupported\s*=\s*true",
            "CompactSteamGuardLargeInputSupported\s*=\s*true",
            "CompactSteamGuardActionFirstSupported\s*=\s*true",
            "CompactSteamGuardInlineActionRowSupported\s*=\s*true",
            "CompactResponsiveSteamGuardActionLayoutSupported\s*=\s*true",
            "ViewportAwareCompactSteamGuardActionRowReflowSupported\s*=\s*true",
            "CompactSteamGuardSubmitDetailLabelSupported\s*=\s*true",
            "CompactSteamGuardRetryGuidanceSupported\s*=\s*true",
            "CompactSteamGuardBoundedHelperSupported\s*=\s*true",
            "CompactPrimaryRetryActionSupported\s*=\s*true",
            "CompactStructuredRetryActionLabelsSupported\s*=\s*true",
            "CompactPrimaryLoginActionFirstSupported\s*=\s*true",
            "CompactAndroidLoginPrimaryCtaSupported\s*=\s*true",
            "CompactAndroidLoginDetailLabelSupported\s*=\s*true",
            "CompactAndroidLoginHelperDetailLabelSupported\s*=\s*true",
            "CompactCompletedAuthSectionSuppressionSupported\s*=\s*true",
            "TouchFirstActionTargetsSupported\s*=\s*true",
            "PrimaryActionWordingSupported\s*=\s*true",
            "ConsistentStartGameCtaSupported\s*=\s*true",
            "CompactLaunchDetailLabelSupported\s*=\s*true",
            "BrandedAtmosphericBackgroundSupported\s*=\s*true",
            "BrandedBackgroundExplicitRgbaSupported\s*=\s*true",
            "HighContrastRoundedActionsSupported\s*=\s*true",
            "CompactHeaderChromeReductionSupported\s*=\s*true",
            "CompactCondensedBrandHeaderSupported\s*=\s*true",
            "CompactSingleLineBrandHeaderSupported\s*=\s*true",
            "CompactReadableBrandSubtitleSupported\s*=\s*true",
            "CompactSectionHeaderSubtitleSuppressionSupported\s*=\s*true",
            "CompactLowProfileSectionHeadersSupported\s*=\s*true",
            "CompactSingleRowSectionHeadersSupported\s*=\s*true",
            "CompactSectionHeaderTaskCueSupported\s*=\s*true",
            "CompactReadableSectionHeaderCuesSupported\s*=\s*true",
            "CompactExplicitSectionHeaderCuesSupported\s*=\s*true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.InstallCloud.cs" `
        "declares install, ready-state, cloud-safety, drawer, and scroll support flags" `
        @(
            "CompactInstallPrimaryActionFirstSupported\s*=\s*true",
            "CompactInstallPrimaryDetailLabelSupported\s*=\s*true",
            "CompactDownloadProgressHeroSupported\s*=\s*true",
            "CompactDownloadProgressStatusLabelSupported\s*=\s*true",
            "CompactReadableDownloadProgressBarSupported\s*=\s*true",
            "CompactInlineInstallVersionControlsSupported\s*=\s*true",
            "CompactVersionDetailsCollapsibleSupported\s*=\s*true",
            "CompactVersionDrawerDetailLabelsSupported\s*=\s*true",
            "CompactVersionDrawerBoundedHelpLabelSupported\s*=\s*true",
            "CompactSelectedVersionSummarySupported\s*=\s*true",
            "CompactSelectedVersionSummaryShortcutSupported\s*=\s*true",
            "CompactSelectedVersionHeadlineSupported\s*=\s*true",
            "CompactResponsiveSelectedVersionSummarySupported\s*=\s*true",
            "CompactStructuredInstallVersionActionLabelsSupported\s*=\s*true",
            "CompactReadyVersionSummarySupported\s*=\s*true",
            "CompactReadyVersionSummaryPanelSupported\s*=\s*true",
            "CompactReadyVersionSummaryShortcutSupported\s*=\s*true",
            "CompactReadyVersionSummaryHeadlineSupported\s*=\s*true",
            "CompactResponsiveReadyVersionSummarySupported\s*=\s*true",
            "CompactReadyStatePrioritySupported\s*=\s*true",
            "CompactReadyStateCloudOptionsBelowLaunchSupported\s*=\s*true",
            "CompactPlaySyncDrawerDetailLabelsSupported\s*=\s*true",
            "CompactStructuredPlaySyncActionLabelsSupported\s*=\s*true",
            "CompactPlainLanguagePlaySyncLabelsSupported\s*=\s*true",
            "CompactReadyStateInstallSectionSuppressionSupported\s*=\s*true",
            "CompactTouchSafeVersionDropdownSupported\s*=\s*true",
            "CompactTouchSafeDropdownPopupSupported\s*=\s*true",
            "CompactCloudSafetyCollapsibleSupported\s*=\s*true",
            "CompactCloudSafetyCueBeforeActionsSupported\s*=\s*true",
            "CompactCloudSafetyDetailLabelSupported\s*=\s*true",
            "CompactCloudOptionsCollapsibleSupported\s*=\s*true",
            "PrimaryCloudActionsBeforeCloudOptionsSupported\s*=\s*true",
            "SaferPullBeforePushOrderingSupported\s*=\s*true",
            "CompactCloudDirectionLabelsSupported\s*=\s*true",
            "CompactCloudPrimaryActionsRowSupported\s*=\s*true",
            "CompactCloudPullDetailLabelSupported\s*=\s*true",
            "CompactCloudPushLockDetailLabelsSupported\s*=\s*true",
            "CompactCloudPushDangerDetailLabelsSupported\s*=\s*true",
            "CompactCloudPushWarningDetailLabelSupported\s*=\s*true",
            "CompactResponsiveActionRowsSupported\s*=\s*true",
            "ManualPushArmedOverwriteWarningSupported\s*=\s*true",
            "CompactButtonLabelsSupported\s*=\s*true",
            "CompactCloudOptionLabelsSupported\s*=\s*true",
            "CompactCloudOptionDetailLabelsSupported\s*=\s*true",
            "CompactCloudOptionsRowSupported\s*=\s*true",
            "CompactDrawerStateResetSupported\s*=\s*true",
            "CompactDrawerToggleFirstSupported\s*=\s*true",
            "CompactLowProfileDrawerTogglesSupported\s*=\s*true",
            "CompactDenseDrawerToggleHeightSupported\s*=\s*true",
            "CompactTouchSafeDrawerToggleSizingSupported\s*=\s*true",
            "CompactSupportToolsGridSupported\s*=\s*true",
            "CompactSupportToolDetailLabelsSupported\s*=\s*true",
            "CompactRawLogReviewLabelSupported\s*=\s*true",
            "CompactDrawerSelectionCollapseSupported\s*=\s*true",
            "CompactActiveSectionScrollSupported\s*=\s*true",
            "CompactPrimaryActionScrollAnchorsSupported\s*=\s*true",
            "CompactPaddedScrollAnchorsSupported\s*=\s*true",
            "CompactBottomScrollBreathingRoomSupported\s*=\s*true",
            "CompactLowProfileAttributionSupported\s*=\s*true",
            "ViewportAwareKeyboardOffsetSupported\s*=\s*true",
            "KeyboardFocusedInputScrollSupported\s*=\s*true",
            "CompactReadyStateSafeFlowSuppressionSupported\s*=\s*true",
            "CompactActiveTaskSafeFlowSuppressionSupported\s*=\s*true"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Diagnostics.cs" `
        "declares diagnostics, fallback recovery, startup recovery, and validation-boundary support flags" `
        @(
            "NativeFallbackRecoveryActionsStyledSupported\s*=\s*true",
            "NativeFallbackDiagnosticsCollapsedSupported\s*=\s*true",
            "NativeFallbackResponsiveRecoveryRowsSupported\s*=\s*true",
            "StartupRecoveryScrollSafeControlsSupported\s*=\s*true",
            "StartupRecoveryStructuredCompactActionsSupported\s*=\s*true",
            "VersionInstallCloudSeparationGuidanceSupported\s*=\s*true",
            "DiagnosticsConsoleHiddenByDefault\s*=\s*true",
            "DiagnosticsConsoleAutoOpensForDiagnosticsActionsSupported\s*=\s*true",
            "CompactLowProfileDiagnosticsToggleSupported\s*=\s*true",
            "CompactDiagnosticsToggleDetailLabelsSupported\s*=\s*true",
            "CompactStructuredDiagnosticsToggleLabelsSupported\s*=\s*true",
            "PlainLanguageHelpReportCopySupported\s*=\s*true",
            "CompactDiagnosticsScrollHostedSupported\s*=\s*true",
            "CompactReadableDiagnosticsLogSupported\s*=\s*true",
            "CompactBoundedDiagnosticsLogViewportSupported\s*=\s*true",
            "ViewportAwareDiagnosticsLogResizeSupported\s*=\s*true",
            "StartupFallbackRawBannerSuppressed\s*=\s*true",
            "PortalUxDeviceValidated\s*=\s*false"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.cs" `
        "keeps secret-safe portal UX narrative and validation boundary text" `
        @(
            "Status-led launcher portal",
            "compact plain-language status copy for sign-in, ownership check, install-needed, and ready-to-play states",
            "Steam sign-in",
            "Steam Guard",
            "game install",
            "play/sync",
            "low-profile status card",
            "stacked phase and next-action labels",
            "stable one-line non-attention detail row",
            "compact touch-safe responsive numbered workflow step strip with visible two-line step labels, separate readable number badges, and a unified 62px touch height",
            "stays one dense row even on narrow compact viewports",
            "tappable compact workflow steps that directly navigate to visible or fallback task sections",
            "subdued inline current-task jump button and two-line workflow cells that share the 62px compact touch height in a low-profile toolbar shell around the touch-safe sticky compact task header",
            "reflows between inline and stacked layouts after Android rotation or keyboard viewport changes",
            "compact current-task jump button",
            "touch-safe sticky compact task bar",
            "structured two-line app-like task title/detail labels",
            "contextual task detail labels",
            "compact touch-safe confirmation dialogs with wider scroll-safe warning text, contextual confirm/cancel labels, and current-viewport sizing after rotation or keyboard viewport changes",
            "compact Android sign-in primary action before helper copy",
            "compact Android sign-in CTA promoted to a large primary action with Sign in with Steam / Android login detail labels before the readable two-line password-manager safety helper",
            "larger compact Steam Guard code entry and Verify Code controls before helper copy, inline where width allows and stacked full-width on narrow compact viewports",
            "compact Steam Guard submit detail labels that keep Verify Code / Submit once action-first",
            "compact Steam Guard bounded two-line helper labels that keep one-shot/no-storage and newest-code retry guidance below the action row",
            "compact failure recovery promoted to a primary Try Again / Restart task action while cloud controls remain hidden",
            "compact completed-auth section suppression",
            "consistent Start Game primary CTA with compact Start Game / Ready version detail labels",
            "Android compact touch-scale floor for small-device readability",
            "Android-readable shader warmup/loading screen",
            "Android-readable startup status card",
            "verbose native fallback diagnostics collapsed until requested",
            "responsive recovery rows on narrow landscape screens",
            "bounded compact quick-start guide panel",
            "collapsible compact quick-start guidance with structured Quick Start / Get saves first title/detail labels",
            "compact active-task safe-flow suppression after setup",
            "compact install step ordering with selected-version summary and a large Download Version / Local files only primary action before optional version details",
            "compact install primary action detail labels for download, redownload, retry, and disabled downloading states",
            "compact install-version drawer controls with structured title/detail labels that keep the version dropdown and refresh action inline where width allows and stack on narrow compact viewports",
            "collapsible compact version details with structured version-file drawer labels and bounded two-line Files for / Play version helper labels",
            "compact download progress promoted directly under the disabled Downloading primary action with a stable two-line Downloading selected version status label",
            "compact readable selected-version and ready-version summary cards",
            "compact responsive selected-version summary that stays explicit about Cloud unchanged/local-file scope and opens the version drawer from a touch-safe Change shortcut",
            "compact ready-version summary",
            "compact ready-version summary panel",
            "compact responsive ready-version summary with Save Check shortcut opens Save Check from a touch-safe Upload-locked cue without unlocking Push",
            "CompactReadyStatePriorityDescription",
            "compact ready-state priority that keeps the ready summary, Save Check shortcut, and Get-saves-first cloud controls before Start Game while moving version management below the primary launch path",
            "CompactReadyStateCloudOptionsBelowLaunchDescription",
            "compact ready-state cloud options stay below Start Game as an optional save-settings drawer after Get-saves-first cloud controls",
            "compact Play/Sync drawer detail labels rendered as structured title/detail action labels",
            "CompactPlainLanguagePlaySyncLabelsDescription",
            "compact Play/Sync uses plain-language save copy such as Get Steam Saves, Upload Locked, Save Check, and Fixes & Help while keeping upload collapsed and explicit",
            "compact user-facing support tool labels such as Safe Start / Cloud off, Check Files / Updates, Game Versions / Refresh list, Repair Files / Rebuild game, Free Space / Old versions, Help Report / Share details, Last Problem / Open details, and Copy Log / Review first",
            "compact responsive action rows that keep save get/upload actions, save settings, and support tools side-by-side where space allows and stack them into full-width controls on narrow compact viewports",
            "compact ready-state install-section suppression",
            "touch-safe compact version dropdowns with larger opened popup row spacing/padding",
            "collapsible compact Save Check / Get saves first cloud-safety drawer labels shown before get/upload actions with a stable two-line Saves for / Get Steam saves before upload / Upload can overwrite Steam detail label",
            "compact cloud action labels that name Android as the Pull destination and Steam as the Push destination",
            "compact Pull detail labels that keep Get Steam Saves / Download to Android direction-explicit",
            "compact locked Push toggle labels that stay structured as Upload Locked / Review first and Hide Upload / Keep locked",
            "compact dangerous Push detail labels that keep Upload to Steam / Overwrite cloud and Confirm Upload / Overwrite cloud direction-explicit after unlock",
            "compact armed Push overwrite warning rendered as a stable Steam Cloud overwrite / Confirm only after Pull/local saves are verified label",
            "compact drawer state reset",
            "compact touch-safe dense drawer toggles before expanded drawer details",
            "compact drawer collapse after version selection",
            "compact state-transition active-section scroll",
            "compact primary-action scroll anchors",
            "padded compact scroll anchors that keep jumped-to task sections below the sticky header",
            "viewport-aware compact task re-anchoring after rotation or keyboard viewport changes",
            "rounded shared metric scaling",
            "compact bottom scroll breathing room",
            "compact low-profile attribution footer",
            "compact scroll-hosted Help & Reports drawer with structured touch-safe drawer title/detail labels, plain-language help-report and launcher-log status copy, a readable bounded compact diagnostics log viewport, and viewport-aware diagnostics log resizing",
            "compact dense vertical rhythm with single-row section headers and explicit short task cues such as Steam account, Current code, Local files, and Play safely",
            "mobile-first compact panel sizing with dense compact shell padding",
            "viewport-aware keyboard offset refresh",
            "keyboard-focused input scrolling so managed Steam Guard and fallback credential fields stay reachable above the soft keyboard",
            "compact ready-state safe-flow suppression",
            "scroll-safe startup recovery controls",
            "hidden diagnostics drawer with automatic opening for explicit diagnostics actions",
            "styled native fallback recovery actions",
            "ARM64 visual validation"
        )
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
