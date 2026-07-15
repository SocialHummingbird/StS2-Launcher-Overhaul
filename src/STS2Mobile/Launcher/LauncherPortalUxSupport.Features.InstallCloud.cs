using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPortalUxSupport
{
    private static void AddInstallCloudFeatureReports(List<LauncherPortalUxFeature> features)
    {
        features.Add(new LauncherPortalUxFeature("Launcher compact install primary action first supported", CompactInstallPrimaryActionFirstSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact install primary detail label supported", CompactInstallPrimaryDetailLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact download progress hero supported", CompactDownloadProgressHeroSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact download progress status label supported", CompactDownloadProgressStatusLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact readable download progress bar supported", CompactReadableDownloadProgressBarSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact inline install-version controls supported", CompactInlineInstallVersionControlsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact version details collapsible", CompactVersionDetailsCollapsibleSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact version drawer detail labels supported", CompactVersionDrawerDetailLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact version drawer bounded help label supported", CompactVersionDrawerBoundedHelpLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact structured install-version action labels supported", CompactStructuredInstallVersionActionLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact selected-version summary supported", CompactSelectedVersionSummarySupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact version summary cards supported", CompactVersionSummaryCardsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact selected-version summary shortcut supported", CompactSelectedVersionSummaryShortcutSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact selected-version headline supported", CompactSelectedVersionHeadlineSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact responsive selected-version summary supported", CompactResponsiveSelectedVersionSummarySupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact ready-version summary supported", CompactReadyVersionSummarySupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact ready-version summary panel supported", CompactReadyVersionSummaryPanelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact ready-version summary shortcut supported", CompactReadyVersionSummaryShortcutSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact ready-version headline supported", CompactReadyVersionSummaryHeadlineSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact responsive ready-version summary supported", CompactResponsiveReadyVersionSummarySupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Home action priority supported", CompactHomeActionPrioritySupported));
        features.Add(new LauncherPortalUxFeature("Launcher cloud controls isolated to Saves supported", CloudControlsIsolatedToSavesSupported));
        features.Add(new LauncherPortalUxFeature("Launcher version controls isolated to Versions supported", VersionControlsIsolatedToVersionsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Mods controls isolated to Mods supported", ModsControlsIsolatedToModsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher Help controls isolated to Help supported", HelpControlsIsolatedToHelpSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact ready-state install-section suppression supported", CompactReadyStateInstallSectionSuppressionSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact touch-safe version dropdown supported", CompactTouchSafeVersionDropdownSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact touch-safe dropdown popup supported", CompactTouchSafeDropdownPopupSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud-safety guidance collapsible", CompactCloudSafetyCollapsibleSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud-safety cue beside guarded actions supported", CompactCloudSafetyCueBesideActionsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud-safety detail label supported", CompactCloudSafetyDetailLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud options collapsible", CompactCloudOptionsCollapsibleSupported));
        features.Add(new LauncherPortalUxFeature("Launcher primary cloud actions before cloud options", PrimaryCloudActionsBeforeCloudOptionsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher safer Pull-before-Push cloud ordering supported", SaferPullBeforePushOrderingSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud direction labels supported", CompactCloudDirectionLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud primary actions row supported", CompactCloudPrimaryActionsRowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact Pull detail label supported", CompactCloudPullDetailLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact locked Push detail labels supported", CompactCloudPushLockDetailLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact dangerous Push detail labels supported", CompactCloudPushDangerDetailLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact armed Push warning detail label supported", CompactCloudPushWarningDetailLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact responsive action rows supported", CompactResponsiveActionRowsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher manual Push armed overwrite warning supported", ManualPushArmedOverwriteWarningSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact button labels supported", CompactButtonLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud option labels supported", CompactCloudOptionLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud option detail labels supported", CompactCloudOptionDetailLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact cloud options row supported", CompactCloudOptionsRowSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact drawer state reset supported", CompactDrawerStateResetSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact drawer toggle-first ordering supported", CompactDrawerToggleFirstSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile drawer toggles supported", CompactLowProfileDrawerTogglesSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact dense drawer toggle height supported", CompactDenseDrawerToggleHeightSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact touch-safe drawer toggle sizing supported", CompactTouchSafeDrawerToggleSizingSupported));
        features.Add(new LauncherPortalUxFeature("Launcher destination support tools supported", DestinationSupportToolsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact support tool detail labels supported", CompactSupportToolDetailLabelsSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact launcher-log review label supported", CompactRawLogReviewLabelSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact drawer selection collapse supported", CompactDrawerSelectionCollapseSupported));
        features.Add(new LauncherPortalUxFeature("Launcher destination page-origin reset supported", DestinationPageOriginResetSupported));
        features.Add(new LauncherPortalUxFeature("Launcher legacy primary-action scroll anchors removed", LegacyPrimaryActionScrollAnchorsRemoved));
        features.Add(new LauncherPortalUxFeature("Launcher legacy padded scroll anchors removed", LegacyPaddedScrollAnchorsRemoved));
        features.Add(new LauncherPortalUxFeature("Launcher compact bottom scroll breathing room supported", CompactBottomScrollBreathingRoomSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact low-profile attribution footer supported", CompactLowProfileAttributionSupported));
        features.Add(new LauncherPortalUxFeature("Launcher viewport-aware keyboard offset supported", ViewportAwareKeyboardOffsetSupported));
        features.Add(new LauncherPortalUxFeature("Launcher keyboard-focused input scroll supported", KeyboardFocusedInputScrollSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact ready-state safe-flow suppression supported", CompactReadyStateSafeFlowSuppressionSupported));
        features.Add(new LauncherPortalUxFeature("Launcher compact active-task safe-flow suppression supported", CompactActiveTaskSafeFlowSuppressionSupported));
    }
}
