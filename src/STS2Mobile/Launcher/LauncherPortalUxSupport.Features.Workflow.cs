using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalUxSupport
{
    private static void AddWorkflowFeatureReports(List<LauncherPortalUxFeature> features)
    {
        features.Add(new LauncherPortalUxFeature("Launcher compact workflow step strip supported", CompactWorkflowStepStripSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact two-column workflow step strip supported", CompactTwoColumnWorkflowStripSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact single-row numbered workflow step strip supported", CompactSingleRowNumberedWorkflowStripSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact narrow workflow single-row supported", CompactNarrowWorkflowSingleRowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact visible workflow step labels supported", CompactVisibleWorkflowStepLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact workflow step detail labels supported", CompactWorkflowStepDetailLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact workflow step number badges supported", CompactWorkflowStepNumberBadgesSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact readable workflow step number badges supported", CompactReadableWorkflowStepNumberBadgesSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact workflow unified touch height supported", CompactWorkflowUnifiedTouchHeightSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact sticky workflow step strip supported", CompactStickyWorkflowStepStripSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile workflow step strip supported", CompactLowProfileWorkflowStepStripSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile two-column workflow step strip supported", CompactLowProfileTwoColumnWorkflowStepStripSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact workflow step direct navigation supported", CompactWorkflowStepDirectNavigationSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact current-task jump supported", CompactCurrentTaskJumpSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact sticky current-task bar supported", CompactStickyCurrentTaskBarSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile current-task bar supported", CompactLowProfileCurrentTaskBarSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact dense inline current-task bar supported", CompactDenseInlineCurrentTaskBarSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact current-task shared touch height supported", CompactCurrentTaskSharedTouchHeightSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile stacked current-task bar supported", CompactLowProfileStackedCurrentTaskBarSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact current-task context labels supported", CompactCurrentTaskContextLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact structured current-task labels supported", CompactStructuredCurrentTaskLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact current-task short title labels supported", CompactCurrentTaskShortTitleLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact touch-safe sticky header controls supported", CompactTouchSafeStickyHeaderControlsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact grouped sticky task header supported", CompactGroupedStickyTaskHeaderSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact sticky task toolbar shell supported", CompactStickyTaskToolbarShellSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact inline sticky task header supported", CompactInlineStickyTaskHeaderSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact responsive sticky task header supported", CompactResponsiveStickyTaskHeaderSupported));
        features.Add(new LauncherPortalUxFeature("Launcher viewport-aware sticky task header reflow supported", ViewportAwareStickyTaskHeaderReflowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher viewport-aware compact task re-anchor supported", ViewportAwareCompactTaskReanchorSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact dense sticky task header supported", CompactDenseStickyTaskHeaderSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact task-jump navigation labels supported", CompactTaskJumpNavigationLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact readable detail label font supported", CompactReadableDetailLabelFontSupported));
    }
}
