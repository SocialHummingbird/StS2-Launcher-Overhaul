function Add-SteamVersionSelectionPortalUxStatusFlagChecks {
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
}
