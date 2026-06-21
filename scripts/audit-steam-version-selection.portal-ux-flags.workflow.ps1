function Add-SteamVersionSelectionPortalUxWorkflowFlagChecks {
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
}
