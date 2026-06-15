namespace STS2Mobile.Launcher;

internal static class LauncherPortalUxSupport
{
    internal const bool StatusLedPortalSupported = true;
    internal const bool PhaseLabelStatusSupported = LauncherPortalStatusFormatter.PhaseLabelStatusSupported;
    internal const bool StructuredStatusChipSupported = LauncherPortalStatusFormatter.StructuredStatusChipSupported;
    internal const bool GuidedNextActionStatusSupported = LauncherPortalStatusFormatter.GuidedNextActionStatusSupported;
    internal const bool ErrorFirstGuidedStatusSupported = LauncherPortalStatusFormatter.ErrorFirstGuidedStatusSupported;
    internal const bool TitledStateSectionsSupported = true;
    internal const bool SafeFirstRunGuidanceSupported = true;
    internal const bool CompactSafeFlowCollapsibleSupported = true;
    internal const bool MobileFirstCompactLayoutSupported = true;
    internal const bool CompactDynamicContentWidthSupported = true;
    internal const bool TabletWideContentLayoutSupported = true;
    internal const bool PortalTopAnchoredContentSupported = true;
    internal const bool CompactVerticalStatusHeroSupported = true;
    internal const bool TouchFirstActionTargetsSupported = true;
    internal const bool PrimaryActionWordingSupported = true;
    internal const bool ConsistentStartGameCtaSupported = true;
    internal const bool BrandedAtmosphericBackgroundSupported = true;
    internal const bool BrandedBackgroundExplicitRgbaSupported = true;
    internal const bool HighContrastRoundedActionsSupported = true;
    internal const bool CompactHeaderChromeReductionSupported = true;
    internal const bool CompactSectionHeaderSubtitleSuppressionSupported = true;
    internal const bool CompactVersionDetailsCollapsibleSupported = true;
    internal const bool CompactCloudSafetyCollapsibleSupported = true;
    internal const bool CompactCloudOptionsCollapsibleSupported = true;
    internal const bool PrimaryCloudActionsBeforeCloudOptionsSupported = true;
    internal const bool SaferPullBeforePushOrderingSupported = true;
    internal const bool ManualPushArmedOverwriteWarningSupported = true;
    internal const bool CompactButtonLabelsSupported = true;
    internal const bool VersionInstallCloudSeparationGuidanceSupported = true;
    internal const bool DiagnosticsConsoleHiddenByDefault = true;
    internal const bool StartupFallbackRawBannerSuppressed = true;
    internal const bool PortalUxDeviceValidated = false;

    internal const string Model =
        "Status-led launcher portal with titled Steam sign-in, Steam Guard, game install, play/sync, and diagnostics sections.";

    internal const string CurrentImplementation =
        "The launcher shell uses a branded atmospheric backdrop with explicit RGBA drawing colors, stronger rounded high-contrast actions, a branded header, readable phase-labeled status capsule with a structured phase chip and error-first guided next-action label, compact vertical next-step status hero, task-led primary action wording, consistent START GAME primary CTA, touch-first action targets, safe first-run guidance with compact phone wording, collapsible compact safe-flow guidance, collapsible compact version details, collapsible compact cloud-safety guidance, collapsible compact cloud options, primary cloud actions before lower-frequency cloud options, safer Pull-before-Push cloud action ordering, armed Push overwrite warning, compact button labels, reduced compact header chrome, compact section-header subtitle suppression, dynamic compact content width, tablet/wide-screen responsive content width, top-anchored portal content, mobile-first compact panel sizing, explicit titled state sections, hidden diagnostics drawer, suppressed raw startup fallback banner, ready-state version-install/cloud-save separation guidance, and branch/cloud helper text while preserving existing SteamKit, download, cloud, and launch flows.";

    internal const string ValidationBoundary =
        "Portal layout, scaling, readability, native login panel placement, hidden diagnostics behavior, and next-action clarity still require ARM64 visual validation on real devices.";
}
