using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalUxSupport
{
    private static void AddStatusFeatureReports(List<LauncherPortalUxFeature> features)
    {
        features.Add(new LauncherPortalUxFeature("Launcher status-led portal supported", StatusLedPortalSupported));
        features.Add(new LauncherPortalUxFeature("Launcher phase-labeled status supported", PhaseLabelStatusSupported));
        features.Add(new LauncherPortalUxFeature("Launcher structured status chip supported", StructuredStatusChipSupported));
        features.Add(new LauncherPortalUxFeature("Launcher guided next-action status supported", GuidedNextActionStatusSupported));
        features.Add(new LauncherPortalUxFeature("Launcher error-first guided status supported", ErrorFirstGuidedStatusSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact plain-language status copy supported", CompactPlainLanguageStatusCopySupported));
        features.Add(new LauncherPortalUxFeature("Launcher titled state sections supported", TitledStateSectionsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher safe first-run guidance supported", SafeFirstRunGuidanceSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact safe-flow guidance collapsible", CompactSafeFlowCollapsibleSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile safe-flow toggle supported", CompactLowProfileSafeFlowToggleSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact safe-flow toggle detail labels supported", CompactSafeFlowToggleDetailLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact structured safe-flow toggle labels supported", CompactStructuredSafeFlowToggleLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact safe-flow bounded guide supported", CompactSafeFlowBoundedGuideSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact plain-language Quick Start labels supported", CompactPlainLanguageQuickStartLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher mobile-first compact layout supported", MobileFirstCompactLayoutSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact dense panel padding supported", CompactDensePanelPaddingSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact dense vertical rhythm supported", CompactDenseVerticalRhythmSupported));
        features.Add(new LauncherPortalUxFeature("Launcher rounded scaled metrics supported", RoundedScaledLauncherMetricsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Android compact touch-scale floor supported", AndroidCompactTouchScaleFloorSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Android-readable warmup screen supported", AndroidReadableWarmupScreenSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Android-readable startup status card supported", AndroidReadableStartupStatusCardSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact dynamic content width supported", CompactDynamicContentWidthSupported));
        features.Add(new LauncherPortalUxFeature("Launcher tablet/wide content layout supported", TabletWideContentLayoutSupported));
        features.Add(new LauncherPortalUxFeature("Launcher top-anchored portal content supported", PortalTopAnchoredContentSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact vertical status hero supported", CompactVerticalStatusHeroSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact stacked status header supported", CompactStackedStatusHeaderSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile status card supported", CompactLowProfileStatusCardSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact status headline row supported", CompactStatusHeadlineRowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact stacked status headline supported", CompactStackedStatusHeadlineSupported));
        features.Add(new LauncherPortalUxFeature("Launcher viewport-aware compact status headline reflow supported", ViewportAwareCompactStatusHeadlineReflowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact stable status detail row supported", CompactStableStatusDetailRowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact short status details supported", CompactShortStatusDetailsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact status tap-to-expand details supported", CompactStatusTapToExpandDetailsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact touch-safe status detail button supported", CompactTouchSafeStatusDetailButtonSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact status detail cue supported", CompactStatusDetailCueSupported));
    }
}
