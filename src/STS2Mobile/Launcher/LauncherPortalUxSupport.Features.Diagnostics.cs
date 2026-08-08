using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalUxSupport
{
    private static void AddDiagnosticsFeatureReports(List<LauncherPortalUxFeature> features)
    {
        features.Add(new LauncherPortalUxFeature("Launcher native fallback recovery actions styled", NativeFallbackRecoveryActionsStyledSupported));
        features.Add(new LauncherPortalUxFeature("Launcher native fallback diagnostics collapsed by default", NativeFallbackDiagnosticsCollapsedSupported));
        features.Add(new LauncherPortalUxFeature("Launcher native fallback responsive recovery rows supported", NativeFallbackResponsiveRecoveryRowsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher startup recovery scroll-safe controls supported", StartupRecoveryScrollSafeControlsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher startup recovery structured compact actions supported", StartupRecoveryStructuredCompactActionsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher version-install/cloud-save separation guidance supported", VersionInstallCloudSeparationGuidanceSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Help & Reports drawer hidden by default", DiagnosticsConsoleHiddenByDefault));
        features.Add(new LauncherPortalUxFeature("Launcher Help & Reports drawer auto-opens for diagnostics actions", DiagnosticsConsoleAutoOpensForDiagnosticsActionsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile diagnostics toggle supported", CompactLowProfileDiagnosticsToggleSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact diagnostics toggle detail labels supported", CompactDiagnosticsToggleDetailLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact structured diagnostics toggle labels supported", CompactStructuredDiagnosticsToggleLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher plain-language help report copy supported", PlainLanguageHelpReportCopySupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact diagnostics scroll-hosted supported", CompactDiagnosticsScrollHostedSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact readable diagnostics log supported", CompactReadableDiagnosticsLogSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact bounded diagnostics log viewport supported", CompactBoundedDiagnosticsLogViewportSupported));
        features.Add(new LauncherPortalUxFeature("Launcher viewport-aware diagnostics log resize supported", ViewportAwareDiagnosticsLogResizeSupported));
        features.Add(new LauncherPortalUxFeature("Launcher startup fallback raw banner suppressed", StartupFallbackRawBannerSuppressed));
        features.Add(new LauncherPortalUxFeature("Launcher portal UX device validated", PortalUxDeviceValidated));
    }
}
