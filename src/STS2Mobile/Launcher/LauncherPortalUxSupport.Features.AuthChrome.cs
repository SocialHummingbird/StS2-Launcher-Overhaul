using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalUxSupport
{
    private static void AddAuthChromeFeatureReports(List<LauncherPortalUxFeature> features)
    {
        features.Add(new LauncherPortalUxFeature("Launcher compact touch-safe confirmation dialogs supported", CompactTouchSafeConfirmationDialogsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact scroll-safe confirmation dialogs supported", CompactScrollSafeConfirmationDialogsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact contextual confirmation labels supported", CompactContextualConfirmationLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher viewport-aware confirmation dialogs supported", ViewportAwareConfirmationDialogsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Steam Guard large input supported", CompactSteamGuardLargeInputSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Steam Guard action-first layout supported", CompactSteamGuardActionFirstSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Steam Guard inline action row supported", CompactSteamGuardInlineActionRowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact responsive Steam Guard action layout supported", CompactResponsiveSteamGuardActionLayoutSupported));
        features.Add(new LauncherPortalUxFeature("Launcher viewport-aware compact Steam Guard action row reflow supported", ViewportAwareCompactSteamGuardActionRowReflowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Steam Guard submit detail label supported", CompactSteamGuardSubmitDetailLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Steam Guard retry guidance supported", CompactSteamGuardRetryGuidanceSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Steam Guard bounded helper supported", CompactSteamGuardBoundedHelperSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact primary retry action supported", CompactPrimaryRetryActionSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact structured retry action labels supported", CompactStructuredRetryActionLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact primary login action first supported", CompactPrimaryLoginActionFirstSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Android login primary CTA supported", CompactAndroidLoginPrimaryCtaSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Android login detail label supported", CompactAndroidLoginDetailLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Android login helper detail label supported", CompactAndroidLoginHelperDetailLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact completed-auth section suppression supported", CompactCompletedAuthSectionSuppressionSupported));
        features.Add(new LauncherPortalUxFeature("Launcher touch-first action targets supported", TouchFirstActionTargetsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher primary action wording supported", PrimaryActionWordingSupported));
        features.Add(new LauncherPortalUxFeature("Launcher consistent Start Game CTA supported", ConsistentStartGameCtaSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact launch detail label supported", CompactLaunchDetailLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher branded atmospheric background supported", BrandedAtmosphericBackgroundSupported));
        features.Add(new LauncherPortalUxFeature("Launcher branded background explicit RGBA supported", BrandedBackgroundExplicitRgbaSupported));
        features.Add(new LauncherPortalUxFeature("Launcher high-contrast rounded actions supported", HighContrastRoundedActionsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact header chrome reduction supported", CompactHeaderChromeReductionSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact condensed brand header supported", CompactCondensedBrandHeaderSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact single-line brand header supported", CompactSingleLineBrandHeaderSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact readable brand subtitle supported", CompactReadableBrandSubtitleSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact section-header subtitle suppression supported", CompactSectionHeaderSubtitleSuppressionSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile section headers supported", CompactLowProfileSectionHeadersSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact single-row section headers supported", CompactSingleRowSectionHeadersSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact section-header task cues supported", CompactSectionHeaderTaskCueSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact readable section-header cues supported", CompactReadableSectionHeaderCuesSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact explicit section-header cues supported", CompactExplicitSectionHeaderCuesSupported));
    }
}
