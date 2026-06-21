function Add-SteamVersionSelectionPortalUxAuthChromeFlagChecks {
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
}
