using System.IO;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const string MissingDiagnosticValue = "<none>";

    internal sealed partial class Snapshot
    {
        internal Snapshot(
            string dataDir,
            string accountName,
            bool hasSavedCredentials,
            bool gameFilesReady,
            string sessionState,
            string failReason
        )
        {
            _state = new LauncherStateReport(
                dataDir,
                accountName,
                hasSavedCredentials,
                gameFilesReady,
                sessionState,
                failReason
            );
        }

        private readonly LauncherStateReport _state;

        internal string WriteDiagnosticsReport()
            => CreateTimestampedText(
                "StS2 Mobile diagnostics",
                GeneratedUtcLabel,
                AppendFullLauncherDiagnostics
            ).Write(
                "sts2-mobile-diagnostics",
                _state.DataDir
            );

        private void AppendFullLauncherDiagnostics(StringBuilder sb)
        {
            AppendPublicSharingWarning(sb);
            AppendLauncherState(sb, LauncherStateDetail.Detailed);
            AppendLauncherPreferences(sb, _state.DataDir);
            AppendFullReportDiagnostics(sb, _state.DataDir);
        }
    }

    private static void AppendPublicSharingWarning(StringBuilder sb)
    {
        sb.AppendLine("Public sharing warning: review and redact this diagnostics report before posting publicly.");
        sb.AppendLine("It may include account names, local paths, device details, save/cloud state, and log excerpts.");
        sb.AppendLine();
    }

    private static void AppendLauncherPreferences(StringBuilder sb, string dataDir)
    {
        var preferences = LauncherPreferences.ReadActionPreferences();
        var branch = SteamGameBranch.Normalize(preferences.GameBranch);
        sb.AppendLine($"Cloud sync pref: {preferences.CloudSyncEnabled}");
        sb.AppendLine($"Local backup pref: {preferences.LocalBackupEnabled}");
        sb.AppendLine($"Selected game branch: {branch}");
        sb.AppendLine($"Selected game branch preference key: game_branch");
        sb.AppendLine($"Selected game branch source: {(LauncherPreferences.GameBranchPreferenceExists() ? "saved preference" : "default fallback")}");
        sb.AppendLine($"Selected game branch selection kind: {SteamGameBranch.SelectionKind(branch)}");
        sb.AppendLine($"Selected game version name: {SteamGameBranch.DisplayName(branch)}");
        sb.AppendLine($"Selected game version note: {SteamGameBranch.SelectorHelpText(branch)}");
        sb.AppendLine($"Steam branch selector mode: {SteamGameBranch.SelectorMode}");
        sb.AppendLine($"Steam branch discovery supported: {BoolText(SteamGameBranch.BranchDiscoverySupported)}");
        var discoveredBranches = LauncherBranchCatalog.ReadVisibleBranches(dataDir);
        sb.AppendLine($"Steam branch catalog source: {LauncherBranchCatalog.SourceDescription(dataDir)}");
        sb.AppendLine($"Steam branch dropdown options: {LauncherBranchCatalog.DropdownOptionLabels(branch, discoveredBranches)}");
        sb.AppendLine($"Steam branch dropdown option metadata: {LauncherBranchCatalog.DropdownOptionMetadata(branch, discoveredBranches)}");
        sb.AppendLine($"Steam beta password entry supported: {BoolText(SteamGameBranch.BetaPasswordEntrySupported)}");
        sb.AppendLine($"Android credential provider model: {LauncherCredentialEntrySupport.ProviderModel}");
        sb.AppendLine($"Native integrated credential panel supported: {BoolText(LauncherCredentialEntrySupport.NativeIntegratedCredentialPanelSupported)}");
        sb.AppendLine($"Native credential fields Autofill hints configured: {BoolText(LauncherCredentialEntrySupport.NativeCredentialFieldsAutofillHintsConfigured)}");
        sb.AppendLine($"Steam credential web domain configured: {BoolText(LauncherCredentialEntrySupport.SteamCredentialWebDomainConfigured)}");
        sb.AppendLine($"Native credential panel inline status configured: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelInlineStatusConfigured)}");
        sb.AppendLine($"Native credential panel keyboard-safe layout configured: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelKeyboardSafeLayoutConfigured)}");
        sb.AppendLine($"Native credential panel IME inset scroll supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelImeInsetScrollSupported)}");
        sb.AppendLine($"Native credential panel touch-target layout configured: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelTouchTargetLayoutConfigured)}");
        sb.AppendLine($"Native credential panel requests both Autofill fields: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelRequestsBothAutofillFields)}");
        sb.AppendLine($"Native credential panel focus Autofill requests supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelFocusAutofillRequestsSupported)}");
        sb.AppendLine($"Native credential panel task-led buttons supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelTaskLedButtonsSupported)}");
        sb.AppendLine($"Native credential panel password visibility toggle supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelPasswordVisibilityToggleSupported)}");
        sb.AppendLine($"Native credential panel password-focus button supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelPasswordFocusButtonSupported)}");
        sb.AppendLine($"Native credential panel Back dismiss supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelBackDismissSupported)}");
        sb.AppendLine($"Native credential panel dismiss retry supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelDismissRetrySupported)}");
        sb.AppendLine($"Native credential panel dismiss hides keyboard: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelDismissHidesKeyboardSupported)}");
        sb.AppendLine($"Native credential panel suppresses pre-auth save prompt: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelSuppressesPreAuthSavePrompt)}");
        sb.AppendLine($"Steam Guard one-shot code guidance supported: {BoolText(LauncherCredentialEntrySupport.SteamGuardOneShotCodeGuidanceSupported)}");
        sb.AppendLine($"Failed-login retry guidance supported: {BoolText(LauncherCredentialEntrySupport.FailedLoginRetryGuidanceSupported)}");
        sb.AppendLine($"Context-specific login recovery guidance supported: {BoolText(LauncherCredentialEntrySupport.ContextSpecificLoginRecoveryGuidanceSupported)}");
        sb.AppendLine($"Godot login field credential metadata configured: {BoolText(LauncherCredentialEntrySupport.GodotFieldCredentialMetadataConfigured)}");
        sb.AppendLine($"Android keyboard credential hints configured: {BoolText(LauncherCredentialEntrySupport.AndroidKeyboardCredentialHintsConfigured)}");
        sb.AppendLine($"Godot fields are native Android Autofill targets: {BoolText(LauncherCredentialEntrySupport.GodotFieldsAreNativeAndroidAutofillTargets)}");
        sb.AppendLine($"Password-manager suggestions device validated: {BoolText(LauncherCredentialEntrySupport.PasswordManagerSuggestionsDeviceValidated)}");
        sb.AppendLine($"Native credential handoff popup supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialHandoffPopupSupported)}");
        sb.AppendLine($"Launcher stores Steam password for credential providers: {BoolText(LauncherCredentialEntrySupport.AppStoresSteamPassword)}");
        sb.AppendLine($"Native credential handoff result TTL seconds: {LauncherCredentialEntrySupport.NativeCredentialHandoffResultTtlSeconds}");
        sb.AppendLine($"Android credential provider implementation note: {LauncherCredentialEntrySupport.CurrentImplementation}");
        sb.AppendLine($"Android credential provider capability boundary: {LauncherCredentialEntrySupport.CapabilityBoundary}");
        sb.AppendLine($"Launcher portal UX model: {LauncherPortalUxSupport.Model}");
        sb.AppendLine($"Launcher status-led portal supported: {BoolText(LauncherPortalUxSupport.StatusLedPortalSupported)}");
        sb.AppendLine($"Launcher phase-labeled status supported: {BoolText(LauncherPortalUxSupport.PhaseLabelStatusSupported)}");
        sb.AppendLine($"Launcher structured status chip supported: {BoolText(LauncherPortalUxSupport.StructuredStatusChipSupported)}");
        sb.AppendLine($"Launcher guided next-action status supported: {BoolText(LauncherPortalUxSupport.GuidedNextActionStatusSupported)}");
        sb.AppendLine($"Launcher error-first guided status supported: {BoolText(LauncherPortalUxSupport.ErrorFirstGuidedStatusSupported)}");
        sb.AppendLine($"Launcher titled state sections supported: {BoolText(LauncherPortalUxSupport.TitledStateSectionsSupported)}");
        sb.AppendLine($"Launcher safe first-run guidance supported: {BoolText(LauncherPortalUxSupport.SafeFirstRunGuidanceSupported)}");
        sb.AppendLine($"Launcher compact safe-flow guidance collapsible: {BoolText(LauncherPortalUxSupport.CompactSafeFlowCollapsibleSupported)}");
        sb.AppendLine($"Launcher mobile-first compact layout supported: {BoolText(LauncherPortalUxSupport.MobileFirstCompactLayoutSupported)}");
        sb.AppendLine($"Launcher compact dynamic content width supported: {BoolText(LauncherPortalUxSupport.CompactDynamicContentWidthSupported)}");
        sb.AppendLine($"Launcher tablet/wide content layout supported: {BoolText(LauncherPortalUxSupport.TabletWideContentLayoutSupported)}");
        sb.AppendLine($"Launcher top-anchored portal content supported: {BoolText(LauncherPortalUxSupport.PortalTopAnchoredContentSupported)}");
        sb.AppendLine($"Launcher compact vertical status hero supported: {BoolText(LauncherPortalUxSupport.CompactVerticalStatusHeroSupported)}");
        sb.AppendLine($"Launcher touch-first action targets supported: {BoolText(LauncherPortalUxSupport.TouchFirstActionTargetsSupported)}");
        sb.AppendLine($"Launcher primary action wording supported: {BoolText(LauncherPortalUxSupport.PrimaryActionWordingSupported)}");
        sb.AppendLine($"Launcher consistent START GAME CTA supported: {BoolText(LauncherPortalUxSupport.ConsistentStartGameCtaSupported)}");
        sb.AppendLine($"Launcher branded atmospheric background supported: {BoolText(LauncherPortalUxSupport.BrandedAtmosphericBackgroundSupported)}");
        sb.AppendLine($"Launcher branded background explicit RGBA supported: {BoolText(LauncherPortalUxSupport.BrandedBackgroundExplicitRgbaSupported)}");
        sb.AppendLine($"Launcher high-contrast rounded actions supported: {BoolText(LauncherPortalUxSupport.HighContrastRoundedActionsSupported)}");
        sb.AppendLine($"Launcher compact header chrome reduction supported: {BoolText(LauncherPortalUxSupport.CompactHeaderChromeReductionSupported)}");
        sb.AppendLine($"Launcher compact section-header subtitle suppression supported: {BoolText(LauncherPortalUxSupport.CompactSectionHeaderSubtitleSuppressionSupported)}");
        sb.AppendLine($"Launcher compact version details collapsible: {BoolText(LauncherPortalUxSupport.CompactVersionDetailsCollapsibleSupported)}");
        sb.AppendLine($"Launcher compact cloud-safety guidance collapsible: {BoolText(LauncherPortalUxSupport.CompactCloudSafetyCollapsibleSupported)}");
        sb.AppendLine($"Launcher compact cloud options collapsible: {BoolText(LauncherPortalUxSupport.CompactCloudOptionsCollapsibleSupported)}");
        sb.AppendLine($"Launcher primary cloud actions before cloud options: {BoolText(LauncherPortalUxSupport.PrimaryCloudActionsBeforeCloudOptionsSupported)}");
        sb.AppendLine($"Launcher safer Pull-before-Push cloud ordering supported: {BoolText(LauncherPortalUxSupport.SaferPullBeforePushOrderingSupported)}");
        sb.AppendLine($"Launcher manual Push armed overwrite warning supported: {BoolText(LauncherPortalUxSupport.ManualPushArmedOverwriteWarningSupported)}");
        sb.AppendLine($"Launcher compact button labels supported: {BoolText(LauncherPortalUxSupport.CompactButtonLabelsSupported)}");
        sb.AppendLine($"Launcher version-install/cloud-save separation guidance supported: {BoolText(LauncherPortalUxSupport.VersionInstallCloudSeparationGuidanceSupported)}");
        sb.AppendLine($"Launcher diagnostics console hidden by default: {BoolText(LauncherPortalUxSupport.DiagnosticsConsoleHiddenByDefault)}");
        sb.AppendLine($"Launcher startup fallback raw banner suppressed: {BoolText(LauncherPortalUxSupport.StartupFallbackRawBannerSuppressed)}");
        sb.AppendLine($"Launcher portal UX device validated: {BoolText(LauncherPortalUxSupport.PortalUxDeviceValidated)}");
        sb.AppendLine($"Launcher portal UX implementation note: {LauncherPortalUxSupport.CurrentImplementation}");
        sb.AppendLine($"Launcher portal UX validation boundary: {LauncherPortalUxSupport.ValidationBoundary}");
        sb.AppendLine($"SteamKit debug logs opt-in enabled: {BoolText(SteamConnectionConfigurationFactory.SteamKitDebugLogsOptInEnabled)}");
        sb.AppendLine($"SteamKit debug logs sanitized for credentials/tokens: {BoolText(SteamConnectionConfigurationFactory.SteamKitDebugLogsSanitized)}");
        sb.AppendLine($"Selected game branch storage directory: {SteamGameBranch.StateDirectoryName(branch)}");
        sb.AppendLine($"Selected game version slot kind: {SteamGameInstallPaths.VersionSlotKind(branch)}");
        sb.AppendLine($"Selected game version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch)}");
        sb.AppendLine($"Selected game directory: {SteamGameInstallPaths.GameDirectory(dataDir, branch)}");
        sb.AppendLine($"Selected game PCK: {LauncherGameFiles.PckPath(dataDir, branch)}");
        sb.AppendLine($"Selected game files ready: {BoolText(LauncherGameFiles.Ready(dataDir, branch))}");
        sb.AppendLine($"Selected game readiness problem: {ValueOrMissing(LauncherGameFiles.ReadinessProblem(dataDir, branch))}");
        AppendGameRuntimeSlot(sb, dataDir, branch);
        sb.AppendLine($"Selected download state: {SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch)}");
        AppendBranchAvailability(sb, dataDir);
        var branchMarkerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, branch);
        sb.AppendLine($"Selected game branch marker: {branchMarkerPath}");
        sb.AppendLine($"Selected game branch marker present: {BoolText(File.Exists(branchMarkerPath))}");
        sb.AppendLine($"Selected game branch marker branch: {ReadBranchMarkerBranch(branchMarkerPath)}");
        sb.AppendLine($"Selected game branch marker install slot kind: {ReadBranchMarkerValue(branchMarkerPath, "Install slot kind:")}");
        sb.AppendLine($"Selected game branch marker install slot directory: {ReadBranchMarkerValue(branchMarkerPath, "Install slot directory:")}");
        sb.AppendLine($"Selected game branch marker expected install slot kind: {SteamGameInstallPaths.VersionSlotKind(branch)}");
        sb.AppendLine($"Selected game branch marker expected install slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch)}");
        sb.AppendLine($"Selected game branch marker has matching install slot provenance: {BoolText(BranchMarkerHasInstallSlotProvenance(branchMarkerPath, SteamGameInstallPaths.VersionSlotKind(branch), SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch)))}");
        sb.AppendLine($"Selected game branch marker has depot manifests: {BoolText(BranchMarkerHasDepotManifestProvenance(branchMarkerPath))}");
        sb.AppendLine($"Selected game branch marker has branch integrity provenance: {BoolText(BranchMarkerHasIntegrityProvenance(branchMarkerPath))}");
        sb.AppendLine($"Selected game branch marker depot manifest entries: {BranchMarkerDepotManifestCount(branchMarkerPath)}");
        sb.AppendLine($"Selected game branch marker depots matching public: {ReadBranchMarkerValue(branchMarkerPath, "Depot manifests matching public count:")}");
        sb.AppendLine($"Selected game branch marker depots differing from public: {ReadBranchMarkerValue(branchMarkerPath, "Depot manifests differing from public count:")}");
        sb.AppendLine($"Selected game branch marker depots without public comparison: {ReadBranchMarkerValue(branchMarkerPath, "Depot manifests without public comparison count:")}");
        sb.AppendLine($"Selected game branch marker depots inherited from public: {ReadBranchMarkerValue(branchMarkerPath, "Depot manifests inherited from public count:")}");
        sb.AppendLine($"Selected game branch marker depots missing selected branch manifest: {ReadBranchMarkerValue(branchMarkerPath, "Depot manifests missing selected branch manifest count:")}");
        sb.AppendLine($"Selected game branch marker partial Steam branch evidence: {BranchMarkerPartialSteamBranchEvidence(branchMarkerPath)}");
        sb.AppendLine($"Selected game branch marker depot manifest rows: {ReadBranchMarkerValues(branchMarkerPath, "Depot manifest:", 32)}");
        sb.AppendLine($"Selected game branch marker ready: {BoolText(LauncherGameFiles.BranchMarkerReady(dataDir, branch))}");
        AppendBranchSwitchSafety(sb, dataDir);
        AppendCachedGameVersions(sb, dataDir);
    }

    private static string ReadBranchMarkerBranch(string markerPath)
        => ReadBranchMarkerValue(markerPath, "Branch:");

    private static void AppendBranchAvailability(StringBuilder sb, string dataDir)
    {
        var markerPath = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
        sb.AppendLine($"Steam branch availability marker filename: {SteamGameInstallPaths.BranchAvailabilityMarkerFileName}");
        sb.AppendLine($"Steam branch availability marker path: {markerPath}");
        sb.AppendLine($"Steam branch availability marker present: {BoolText(File.Exists(markerPath))}");
        sb.AppendLine($"Steam branch availability UTC: {ReadBranchAvailabilityMarkerValue(dataDir, "UTC:")}");
        sb.AppendLine($"Steam branch availability selected branch: {ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch:")}");
        sb.AppendLine($"Steam branch availability matches current selected branch: {BoolText(BranchAvailabilityMarkerMatchesSelectedBranch(dataDir))}");
        sb.AppendLine($"Steam branch availability selected branch visibility: {ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch visibility:")}");
        sb.AppendLine($"Steam branch availability selected branch Windows depot manifests: {ReadBranchAvailabilityMarkerValue(dataDir, "Windows depot manifests for selected branch:")}");
        sb.AppendLine($"Steam branch availability selected branch downloadable: {BranchAvailabilitySelectedBranchDownloadable(dataDir)}");
        sb.AppendLine($"Steam branch availability selected branch problem: {BranchAvailabilitySelectedBranchProblem(dataDir)}");
        sb.AppendLine($"Steam branch availability visible branch count: {ReadBranchAvailabilityMarkerValue(dataDir, "Visible branch count:")}");
        sb.AppendLine($"Steam branch availability visible branches: {ReadBranchAvailabilityMarkerValues(dataDir, "Visible branch:")}");
    }

    private static void AppendGameRuntimeSlot(StringBuilder sb, string dataDir, string branch)
    {
        var slot = GameRuntimeSlot.Inspect(dataDir, branch);
        sb.AppendLine($"Selected runtime slot branch: {slot.Branch}");
        sb.AppendLine($"Selected runtime slot display name: {slot.DisplayName}");
        sb.AppendLine($"Selected runtime slot kind: {slot.SlotKind}");
        sb.AppendLine($"Selected runtime slot directory: {slot.SlotDirectory}");
        sb.AppendLine($"Runtime slot evidence marker filename: {LauncherRuntimeSlotEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime slot evidence marker path: {LauncherRuntimeSlotEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime slot evidence marker present: {BoolText(LauncherRuntimeSlotEvidence.MarkerPresent(dataDir))}");
        sb.AppendLine($"Runtime slot evidence selected branch: {LauncherRuntimeSlotEvidence.Branch(dataDir)}");
        sb.AppendLine($"Runtime slot evidence selected branch matches current runtime: {BoolText(LauncherRuntimeSlotEvidence.BranchMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime slot evidence runtime slot ID: {LauncherRuntimeSlotEvidence.RuntimeSlotId(dataDir)}");
        sb.AppendLine($"Runtime slot evidence runtime slot ID matches current runtime: {BoolText(LauncherRuntimeSlotEvidence.RuntimeSlotIdMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime slot evidence selected PCK matches current runtime: {BoolText(LauncherRuntimeSlotEvidence.PckMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime slot evidence selected source sts2.dll matches current runtime: {BoolText(LauncherRuntimeSlotEvidence.SourceAssemblyMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime slot evidence files ready: {LauncherRuntimeSlotEvidence.FilesReady(dataDir)}");
        sb.AppendLine($"Runtime slot evidence readiness problem: {ValueOrMissing(LauncherRuntimeSlotEvidence.ReadinessProblem(dataDir))}");
        sb.AppendLine($"Runtime slot evidence runtime pack usability status: {LauncherRuntimeSlotEvidence.RuntimePackUsabilityStatus(dataDir)}");
        sb.AppendLine($"Runtime slot evidence patch compatibility status: {LauncherRuntimeSlotEvidence.PatchCompatibilityStatus(dataDir)}");
        sb.AppendLine($"Selected runtime game directory: {slot.GameDirectory}");
        sb.AppendLine($"Selected runtime PCK path: {slot.PckPath}");
        sb.AppendLine($"Selected runtime PCK SHA256: {slot.PckSha256}");
        sb.AppendLine($"Selected runtime release info path: {slot.ReleaseInfoPath}");
        sb.AppendLine($"Selected runtime release version: {slot.Metadata.ReleaseVersion}");
        sb.AppendLine($"Selected runtime release commit: {slot.Metadata.ReleaseCommit}");
        sb.AppendLine($"Selected runtime release build id: {slot.Metadata.ReleaseBuildId}");
        sb.AppendLine($"Selected runtime depot manifest count: {slot.Metadata.DepotManifestCount}");
        sb.AppendLine($"Selected runtime depots matching public: {slot.Metadata.DepotsMatchingPublic}");
        sb.AppendLine($"Selected runtime depots differing from public: {slot.Metadata.DepotsDifferingFromPublic}");
        sb.AppendLine($"Selected runtime depots inherited from public: {slot.Metadata.DepotsInheritedFromPublic}");
        sb.AppendLine($"Selected runtime depots missing selected manifest: {slot.Metadata.DepotsMissingSelectedManifest}");
        sb.AppendLine($"Selected runtime depot manifest fingerprint: {slot.Metadata.DepotManifestFingerprint}");
        sb.AppendLine($"Selected runtime identity summary: {slot.Metadata.IdentitySummary}");
        sb.AppendLine($"Selected runtime slot ID: {slot.RuntimeSlotId}");
        sb.AppendLine($"Selected runtime slot identity: {slot.RuntimeSlotIdentity.Replace("\n", " | ")}");
        sb.AppendLine($"Selected runtime source sts2.dll path: {slot.SourceAssemblyPath}");
        sb.AppendLine($"Selected runtime source sts2.dll exists: {BoolText(slot.SourceAssemblyExists)}");
        sb.AppendLine($"Selected runtime source sts2.dll SHA256: {slot.SourceAssemblySha256}");
        sb.AppendLine($"Selected runtime active Android sts2.dll path: {slot.ActiveAndroidAssemblyPath}");
        sb.AppendLine($"Selected runtime active Android sts2.dll exists: {BoolText(slot.ActiveAndroidAssemblyExists)}");
        sb.AppendLine($"Selected runtime active Android sts2.dll SHA256: {slot.ActiveAndroidAssemblySha256}");
        sb.AppendLine($"Selected runtime branch source available: {BoolText(slot.BranchRuntimeAvailable)}");
        sb.AppendLine($"Selected runtime source matches active Android assembly: {BoolText(slot.SourceMatchesActiveAndroidAssembly)}");
        sb.AppendLine($"Selected runtime uses legacy packaged public runtime: {BoolText(slot.UsesLegacyPackagedPublicRuntime)}");
        sb.AppendLine($"Selected runtime pack manifest path: {slot.RuntimePackManifestPath}");
        sb.AppendLine($"Selected runtime pack manifest present: {BoolText(slot.RuntimePackManifestExists)}");
        sb.AppendLine($"Selected runtime pack status: {slot.RuntimePack.Status}");
        sb.AppendLine($"Selected runtime pack usability status: {slot.RuntimePackUsabilityStatus}");
        sb.AppendLine($"Selected runtime pack usable: {BoolText(slot.RuntimePackUsable)}");
        sb.AppendLine($"Selected runtime pack id: {ValueOrMissing(slot.RuntimePack.PackId)}");
        sb.AppendLine($"Selected runtime pack source runtime slot ID: {ValueOrMissing(slot.RuntimePack.SourceRuntimeSlotId)}");
        sb.AppendLine($"Selected runtime pack source runtime slot ID matches selected runtime: {BoolText(slot.RuntimePackSlotIdMatches)}");
        sb.AppendLine($"Selected runtime pack source branch: {ValueOrMissing(slot.RuntimePack.SourceBranch)}");
        sb.AppendLine($"Selected runtime pack source branch matches selected: {BoolText(slot.RuntimePack.BranchMatches)}");
        sb.AppendLine($"Selected runtime pack source PCK SHA256: {ValueOrMissing(slot.RuntimePack.SourcePckSha256)}");
        sb.AppendLine($"Selected runtime pack source assembly SHA256: {ValueOrMissing(slot.RuntimePack.SourceAssemblySha256)}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll path: {slot.RuntimePack.AndroidAssemblyPath}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll exists: {BoolText(slot.RuntimePack.AndroidAssemblyExists)}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll SHA256: {ValueOrMissing(slot.RuntimePack.AndroidAssemblySha256)}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll actual SHA256: {slot.RuntimePack.ActualAndroidAssemblySha256}");
        sb.AppendLine($"Selected runtime pack Android sts2.dll hash matches manifest: {BoolText(slot.RuntimePack.AndroidAssemblyHashMatches)}");
        sb.AppendLine($"Selected runtime pack patch-set version: {ValueOrMissing(slot.RuntimePack.PatchSetVersion)}");
        sb.AppendLine($"Selected runtime pack patch validation status: {ValueOrMissing(slot.RuntimePack.PatchValidationStatus)}");
        sb.AppendLine($"Selected runtime pack patch validation report: {ValueOrMissing(slot.RuntimePack.PatchValidationReport)}");
        sb.AppendLine($"Selected runtime pack validation mode: {ValueOrMissing(slot.RuntimePack.ValidationMode)}");
        sb.AppendLine($"Selected runtime pack validation surface version: {ValueOrMissing(slot.RuntimePack.ValidationSurfaceVersion)}");
        sb.AppendLine($"Selected runtime pack generated from clean directory: {BoolText(slot.RuntimePack.GeneratedFromCleanDirectory)}");
        sb.AppendLine($"Selected runtime pack support assemblies declared: {BoolText(slot.RuntimePack.SupportAssembliesDeclared)}");
        sb.AppendLine($"Selected runtime pack support assemblies: {ValueOrMissing(string.Join(", ", slot.RuntimePack.SupportAssemblies))}");
        sb.AppendLine($"Selected runtime pack support assembly hashes declared: {BoolText(slot.RuntimePack.SupportAssemblySha256Declared)}");
        sb.AppendLine($"Selected runtime pack support assembly hash count: {slot.RuntimePack.SupportAssemblySha256.Count}");
        sb.AppendLine($"Selected runtime pack checked symbol count: {slot.RuntimePack.CheckedSymbolCount}");
        sb.AppendLine($"Selected runtime pack present symbol count: {slot.RuntimePack.PresentSymbolCount}");
        sb.AppendLine($"Selected runtime pack missing symbol count: {slot.RuntimePack.MissingSymbolCount}");
        sb.AppendLine($"Selected runtime pack minimum launcher version: {ValueOrMissing(slot.RuntimePack.MinimumLauncherVersion)}");
        sb.AppendLine($"Selected patch compatibility source: {slot.PatchCompatibility.Source}");
        sb.AppendLine($"Selected patch compatibility marker path: {ValueOrMissing(slot.PatchCompatibility.MarkerPath)}");
        sb.AppendLine($"Selected patch compatibility required: {BoolText(slot.PatchCompatibility.Required)}");
        sb.AppendLine($"Selected patch compatibility evidence present: {BoolText(slot.PatchCompatibility.Exists)}");
        sb.AppendLine($"Selected patch compatibility evidence readable: {BoolText(slot.PatchCompatibility.Readable)}");
        sb.AppendLine($"Selected patch compatibility status: {slot.PatchCompatibility.Status}");
        sb.AppendLine($"Selected patch compatibility detail: {ValueOrMissing(slot.PatchCompatibility.Detail)}");
        sb.AppendLine($"Selected patch compatibility validated branch: {ValueOrMissing(slot.PatchCompatibility.ValidatedBranch)}");
        sb.AppendLine($"Selected patch compatibility branch matches selected: {BoolText(slot.PatchCompatibility.BranchMatches)}");
        sb.AppendLine($"Selected patch compatibility validated PCK SHA256: {ValueOrMissing(slot.PatchCompatibility.ValidatedPckSha256)}");
        sb.AppendLine($"Selected patch compatibility PCK matches selected: {BoolText(slot.PatchCompatibility.PckMatches)}");
        sb.AppendLine($"Selected patch compatibility validated source assembly SHA256: {ValueOrMissing(slot.PatchCompatibility.ValidatedSourceAssemblySha256)}");
        sb.AppendLine($"Selected patch compatibility source assembly matches selected: {BoolText(slot.PatchCompatibility.SourceAssemblyMatches)}");
        sb.AppendLine($"Selected patch compatibility patch-set version: {ValueOrMissing(slot.PatchCompatibility.PatchSetVersion)}");
        sb.AppendLine($"Selected patch compatibility validation mode: {ValueOrMissing(slot.PatchCompatibility.ValidationMode)}");
        sb.AppendLine($"Selected patch compatibility validation surface version: {ValueOrMissing(slot.PatchCompatibility.ValidationSurfaceVersion)}");
        sb.AppendLine($"Selected patch compatibility required symbol count: {slot.PatchCompatibility.RequiredSymbolCount}");
        sb.AppendLine($"Selected patch compatibility checked symbol count: {slot.PatchCompatibility.CheckedSymbolCount}");
        sb.AppendLine($"Selected patch compatibility present symbol count: {slot.PatchCompatibility.PresentSymbolCount}");
        sb.AppendLine($"Selected patch compatibility missing symbol count: {slot.PatchCompatibility.MissingSymbolCount}");
        sb.AppendLine($"Selected patch compatible: {BoolText(slot.PatchCompatible)}");
        sb.AppendLine($"Runtime patch validation marker filename: {LauncherRuntimePatchValidationEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime patch validation marker path: {LauncherRuntimePatchValidationEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime patch validation marker present: {BoolText(LauncherRuntimePatchValidationEvidence.MarkerPresent(dataDir))}");
        sb.AppendLine($"Runtime patch validation UTC: {LauncherRuntimePatchValidationEvidence.Utc(dataDir)}");
        sb.AppendLine($"Runtime patch validation UTC parseable: {BoolText(LauncherRuntimePatchValidationEvidence.UtcParseable(dataDir))}");
        sb.AppendLine($"Runtime patch validation status: {LauncherRuntimePatchValidationEvidence.Status(dataDir)}");
        sb.AppendLine($"Runtime patch validation selected branch: {LauncherRuntimePatchValidationEvidence.SelectedBranch(dataDir)}");
        sb.AppendLine($"Runtime patch validation selected version: {LauncherRuntimePatchValidationEvidence.SelectedVersion(dataDir)}");
        sb.AppendLine($"Runtime patch validation runtime slot ID: {LauncherRuntimePatchValidationEvidence.RuntimeSlotId(dataDir)}");
        sb.AppendLine($"Runtime patch validation selected PCK SHA256: {LauncherRuntimePatchValidationEvidence.SelectedPckSha256(dataDir)}");
        sb.AppendLine($"Runtime patch validation selected source sts2.dll SHA256: {LauncherRuntimePatchValidationEvidence.SelectedSourceAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime patch validation active Android sts2.dll SHA256: {LauncherRuntimePatchValidationEvidence.ActiveAndroidAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime patch validation runtime pack id: {ValueOrMissing(LauncherRuntimePatchValidationEvidence.RuntimePackId(dataDir))}");
        sb.AppendLine($"Runtime patch validation runtime pack status: {LauncherRuntimePatchValidationEvidence.RuntimePackStatus(dataDir)}");
        sb.AppendLine($"Runtime patch validation applied patch count: {LauncherRuntimePatchValidationEvidence.AppliedPatchCount(dataDir)}");
        sb.AppendLine($"Runtime patch validation failed patch count: {LauncherRuntimePatchValidationEvidence.FailedPatchCount(dataDir)}");
        sb.AppendLine($"Runtime patch validation total patch count: {LauncherRuntimePatchValidationEvidence.TotalPatchCount(dataDir)}");
        sb.AppendLine($"Runtime patch validation failure messages: {LauncherRuntimePatchValidationEvidence.FailureMessages(dataDir)}");
        sb.AppendLine($"Runtime cache marker filename: {LauncherRuntimeCacheEvidence.MarkerFileName}");
        sb.AppendLine($"Runtime cache marker path: {LauncherRuntimeCacheEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Runtime cache marker present: {BoolText(LauncherRuntimeCacheEvidence.MarkerPresent(dataDir))}");
        sb.AppendLine($"Runtime cache marker UTC millis: {LauncherRuntimeCacheEvidence.UtcMillis(dataDir)}");
        sb.AppendLine($"Runtime cache marker package: {LauncherRuntimeCacheEvidence.Package(dataDir)}");
        sb.AppendLine($"Runtime cache marker version name: {LauncherRuntimeCacheEvidence.VersionName(dataDir)}");
        sb.AppendLine($"Runtime cache marker version code: {LauncherRuntimeCacheEvidence.VersionCode(dataDir)}");
        sb.AppendLine($"Runtime cache marker assembly cache schema: {LauncherRuntimeCacheEvidence.AssemblyCacheSchema(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected branch: {LauncherRuntimeCacheEvidence.SelectedBranch(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected branch requires runtime pack: {LauncherRuntimeCacheEvidence.SelectedBranchRequiresRuntimePack(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime ID: {LauncherRuntimeCacheEvidence.RuntimeId(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime source: {LauncherRuntimeCacheEvidence.RuntimeSource(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime pack directory: {LauncherRuntimeCacheEvidence.RuntimePackDirectory(dataDir)}");
        sb.AppendLine($"Runtime cache marker runtime pack game assembly: {LauncherRuntimeCacheEvidence.RuntimePackGameAssembly(dataDir)}");
        sb.AppendLine($"Runtime cache marker game directory: {LauncherRuntimeCacheEvidence.GameDirectory(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected PCK path: {LauncherRuntimeCacheEvidence.SelectedPckPath(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected PCK identity: {LauncherRuntimeCacheEvidence.SelectedPckIdentity(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected PCK SHA256: {LauncherRuntimeCacheEvidence.SelectedPckSha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected source sts2.dll: {LauncherRuntimeCacheEvidence.SelectedSourceAssembly(dataDir)}");
        sb.AppendLine($"Runtime cache marker selected source sts2.dll SHA256: {LauncherRuntimeCacheEvidence.SelectedSourceAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker active source sts2.dll: {LauncherRuntimeCacheEvidence.ActiveSourceAssembly(dataDir)}");
        sb.AppendLine($"Runtime cache marker active source sts2.dll SHA256: {LauncherRuntimeCacheEvidence.ActiveSourceAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker publish cache directory: {LauncherRuntimeCacheEvidence.PublishCacheDirectory(dataDir)}");
        sb.AppendLine($"Runtime cache marker publish cache active sts2.dll SHA256: {LauncherRuntimeCacheEvidence.PublishCacheActiveAssemblySha256(dataDir)}");
        sb.AppendLine($"Runtime cache marker matches selected branch: {BoolText(LauncherRuntimeCacheEvidence.MatchesSelectedBranch(dataDir, branch))}");
        sb.AppendLine($"Runtime cache marker selected PCK matches selected runtime: {BoolText(LauncherRuntimeCacheEvidence.PckMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime cache marker selected source sts2.dll matches selected runtime: {BoolText(LauncherRuntimeCacheEvidence.SourceAssemblyMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime cache publish sts2.dll matches selected runtime: {BoolText(LauncherRuntimeCacheEvidence.PublishCacheMatchesSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Runtime cache prepared for selected runtime: {BoolText(LauncherRuntimeCacheEvidence.CachePreparedForSelectedRuntime(dataDir, branch))}");
        sb.AppendLine($"Selected runtime pairing status: {slot.RuntimePairingStatus}");
        sb.AppendLine($"Selected runtime requires usable runtime pack: {BoolText(slot.RequiresRuntimePackOrPreparedCache)}");
        sb.AppendLine($"Selected runtime branch-matched Android runtime prepared: {BoolText(slot.BranchMatchedAndroidRuntimePrepared)}");
        sb.AppendLine($"Selected runtime compatible: {BoolText(slot.RuntimeCompatible)}");
        sb.AppendLine($"Selected runtime playable: {BoolText(slot.Playable)}");
    }

    private static string ReadBranchAvailabilityMarkerValue(string dataDir, string prefix)
        => ReadBranchMarkerValue(SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir), prefix);

    private static bool BranchAvailabilityMarkerMatchesSelectedBranch(string dataDir)
    {
        var markerBranch = ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch:");
        if (markerBranch.StartsWith("<", System.StringComparison.Ordinal))
            return false;

        return string.Equals(
            SteamGameBranch.Normalize(markerBranch),
            SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch()),
            System.StringComparison.OrdinalIgnoreCase
        );
    }

    private static string ReadBranchAvailabilityMarkerValues(string dataDir, string prefix)
    {
        var markerPath = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
        if (!File.Exists(markerPath))
            return MissingDiagnosticValue;

        try
        {
            var values = new System.Collections.Generic.List<string>();
            foreach (var line in File.ReadLines(markerPath))
            {
                if (line.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    values.Add(ValueOrMissing(line.Substring(prefix.Length).Trim()));
            }

            return values.Count == 0 ? $"<missing {prefix.TrimEnd(':')} lines>" : string.Join(" | ", values);
        }
        catch
        {
            return "<read failed>";
        }
    }

    private static string BranchAvailabilitySelectedBranchDownloadable(string dataDir)
        => BranchAvailabilitySelectedBranchManifestCount(dataDir) > 0 ? "true" : "false";

    private static string BranchAvailabilitySelectedBranchProblem(string dataDir)
    {
        var manifestCount = BranchAvailabilitySelectedBranchManifestCount(dataDir);
        if (manifestCount > 0)
            return "downloadable";

        var visibility = ReadBranchAvailabilityMarkerValue(dataDir, "Selected branch visibility:");
        if (visibility.StartsWith("<", System.StringComparison.Ordinal))
            return visibility;

        return visibility.Contains("not listed", System.StringComparison.OrdinalIgnoreCase)
            ? "selected branch was not listed in Steam branch metadata and has no Windows depot manifest"
            : "selected branch was listed but has no Windows depot manifest";
    }

    private static int BranchAvailabilitySelectedBranchManifestCount(string dataDir)
        => int.TryParse(
            ReadBranchAvailabilityMarkerValue(dataDir, "Windows depot manifests for selected branch:"),
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var count
        )
            ? count
            : 0;

    private static string ReadBranchMarkerValue(string markerPath, string prefix)
    {
        if (!File.Exists(markerPath))
            return MissingDiagnosticValue;

        try
        {
            foreach (var line in File.ReadLines(markerPath))
            {
                if (line.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    return ValueOrMissing(line.Substring(prefix.Length).Trim());
            }
        }
        catch
        {
            return "<read failed>";
        }

        return $"<missing {prefix.TrimEnd(':')} line>";
    }

    private static bool BranchMarkerHasDepotManifestProvenance(string markerPath)
        => BranchMarkerDepotManifestCount(markerPath) > 0;

    private static bool BranchMarkerHasIntegrityProvenance(string markerPath)
        => ReadMarkerInt(markerPath, "Depot manifests matching public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests differing from public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests without public comparison count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests inherited from public count:").HasValue
        && ReadMarkerInt(markerPath, "Depot manifests missing selected branch manifest count:").HasValue;

    private static bool BranchMarkerHasInstallSlotProvenance(string markerPath, string expectedSlotKind, string expectedSlotDirectory)
        => string.Equals(
            ReadBranchMarkerValue(markerPath, "Install slot kind:"),
            expectedSlotKind,
            System.StringComparison.OrdinalIgnoreCase
        )
        && string.Equals(
            NormalizeMarkerPath(ReadBranchMarkerValue(markerPath, "Install slot directory:")),
            NormalizeMarkerPath(expectedSlotDirectory),
            System.StringComparison.OrdinalIgnoreCase
        );

    private static string NormalizeMarkerPath(string path)
        => string.IsNullOrWhiteSpace(path) || path.StartsWith("<", System.StringComparison.Ordinal)
            ? string.Empty
            : path.Trim().Replace('\\', '/').TrimEnd('/');

    private static int BranchMarkerDepotManifestCount(string markerPath)
    {
        if (!File.Exists(markerPath))
            return 0;

        try
        {
            var count = 0;
            foreach (var line in File.ReadLines(markerPath))
            {
                if (line.StartsWith("Depot manifest:", System.StringComparison.OrdinalIgnoreCase))
                    count++;
            }
            return count;
        }
        catch
        {
            return 0;
        }
    }

    private static string BranchMarkerPartialSteamBranchEvidence(string markerPath)
    {
        var matching = ReadMarkerInt(markerPath, "Depot manifests matching public count:");
        var differing = ReadMarkerInt(markerPath, "Depot manifests differing from public count:");
        var inherited = ReadMarkerInt(markerPath, "Depot manifests inherited from public count:");
        var selectedMissing = ReadMarkerInt(markerPath, "Depot manifests missing selected branch manifest count:");
        if (!matching.HasValue || !differing.HasValue)
            return MissingDiagnosticValue;

        if ((inherited ?? 0) > 0 && differing.Value > 0)
            return "selected branch inherits public depot manifests and overrides other depots";

        if ((selectedMissing ?? 0) > 0 && differing.Value > 0)
            return "selected branch has missing explicit branch manifests and branch-specific depot manifests";

        if ((inherited ?? 0) > 0 && differing.Value == 0)
            return "selected branch inherits public depot manifests only";

        if (matching.Value > 0 && differing.Value > 0)
            return "selected branch has both public-identical and branch-specific depot manifests";

        if (matching.Value > 0 && differing.Value == 0)
            return "selected branch depot manifests all match public";

        if (matching.Value == 0 && differing.Value > 0)
            return "selected branch depot manifests all differ from public";

        return "selected branch has no public comparison evidence";
    }

    private static string ReadBranchMarkerValues(string markerPath, string prefix, int maxValues)
    {
        if (!File.Exists(markerPath))
            return MissingDiagnosticValue;

        try
        {
            var values = new System.Collections.Generic.List<string>();
            foreach (var line in File.ReadLines(markerPath))
            {
                if (!line.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                values.Add(ValueOrMissing(line.Substring(prefix.Length).Trim()));
                if (values.Count >= maxValues)
                    break;
            }

            return values.Count == 0
                ? $"<missing {prefix.TrimEnd(':')} lines>"
                : string.Join(" | ", values);
        }
        catch
        {
            return "<read failed>";
        }
    }

    private static int? ReadMarkerInt(string markerPath, string prefix)
    {
        var value = ReadBranchMarkerValue(markerPath, prefix);
        return int.TryParse(
            value,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsed
        )
            ? parsed
            : null;
    }

    private static bool CachedBranchMarkerReady(string cacheDirectoryName, string markerBranch, string markerPath, string cachePath)
    {
        if (string.IsNullOrWhiteSpace(markerBranch) || markerBranch.StartsWith("<"))
            return false;

        return string.Equals(
            cacheDirectoryName,
            SteamGameBranch.StateDirectoryName(markerBranch),
            System.StringComparison.OrdinalIgnoreCase
        )
        && BranchMarkerHasDepotManifestProvenance(markerPath)
        && BranchMarkerHasInstallSlotProvenance(markerPath, SteamGameInstallPaths.VersionSlotKind(markerBranch), cachePath)
        && (
            string.Equals(markerBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase)
            || BranchMarkerHasIntegrityProvenance(markerPath)
        );
    }

    private static void AppendBranchSwitchSafety(StringBuilder sb, string dataDir)
    {
        var branchSwitchMarkerPresent = LauncherBranchSwitchSafety.HasMarker(dataDir);
        var importantSaveEvidenceCount = LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir);
        sb.AppendLine($"Branch switch marker filename: {LauncherBranchSwitchSafety.MarkerFileName}");
        sb.AppendLine($"Branch switch marker path: {LauncherBranchSwitchSafety.MarkerPath(dataDir)}");
        sb.AppendLine($"Branch switch marker present: {BoolText(branchSwitchMarkerPresent)}");
        sb.AppendLine($"Branch switch marker UTC: {LauncherBranchSwitchSafety.MarkerUtc(dataDir)}");
        sb.AppendLine($"Branch switch marker UTC parseable: {BoolText(LauncherBranchSwitchSafety.MarkerUtcParseable(dataDir))}");
        sb.AppendLine($"Branch switch previous branch: {LauncherBranchSwitchSafety.PreviousBranch(dataDir)}");
        sb.AppendLine($"Branch switch selected branch: {LauncherBranchSwitchSafety.SelectedBranch(dataDir)}");
        sb.AppendLine($"Branch switch selected branch selection kind: {LauncherBranchSwitchSafety.SelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Branch switch selector mode: {LauncherBranchSwitchSafety.SelectorMode(dataDir)}");
        sb.AppendLine($"Branch switch selected version: {LauncherBranchSwitchSafety.SelectedVersion(dataDir)}");
        sb.AppendLine($"Branch switch selected version slot kind: {LauncherBranchSwitchSafety.SelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Branch switch selected version slot directory: {LauncherBranchSwitchSafety.SelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Branch switch selected branch matches current selected branch: {BoolText(LauncherBranchSwitchSafety.SelectedBranchMatches(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Branch switch selected branch note: {LauncherBranchSwitchSafety.SelectedBranchNote(dataDir)}");
        sb.AppendLine($"Branch switch local backup forced: {BoolText(LauncherBranchSwitchSafety.LocalBackupForced(dataDir))}");
        sb.AppendLine($"Branch switch manual Push requires backup storage: {BoolText(LauncherBranchSwitchSafety.ManualPushRequiresBackupStorage(dataDir))}");
        sb.AppendLine($"Branch switch warning acknowledged: {BoolText(LauncherBranchSwitchSafety.WarningAcknowledged(dataDir))}");
        sb.AppendLine($"Branch switch non-public warning acknowledged: {BoolText(LauncherBranchSwitchSafety.NonPublicBranchWarningAcknowledged(dataDir))}");
        sb.AppendLine($"Branch switch marker has required safety evidence: {BoolText(LauncherBranchSwitchSafety.HasRequiredEvidence(dataDir))}");
        sb.AppendLine($"Branch switch marker has required safety evidence for selected branch: {BoolText(LauncherBranchSwitchSafety.HasRequiredEvidence(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Push requires backup storage after branch switch: {BoolText(branchSwitchMarkerPresent)}");
        sb.AppendLine($"Manual Pull evidence marker filename: {LauncherCloudSyncEvidence.LastManualPullMarkerFileName}");
        sb.AppendLine($"Manual Pull evidence marker path: {LauncherCloudSyncEvidence.LastManualPullMarkerPath(dataDir)}");
        sb.AppendLine($"Manual Pull evidence marker present: {BoolText(File.Exists(LauncherCloudSyncEvidence.LastManualPullMarkerPath(dataDir)))}");
        sb.AppendLine($"Manual Pull evidence UTC: {LauncherCloudSyncEvidence.LastManualPullUtc(dataDir)}");
        sb.AppendLine($"Manual Pull evidence UTC parseable: {BoolText(LauncherCloudSyncEvidence.LastManualPullUtcParseable(dataDir))}");
        sb.AppendLine($"Manual Pull evidence selected branch: {LauncherCloudSyncEvidence.LastManualPullSelectedBranch(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected branch selection kind: {LauncherCloudSyncEvidence.LastManualPullSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selector mode: {LauncherCloudSyncEvidence.LastManualPullSelectorMode(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version: {LauncherCloudSyncEvidence.LastManualPullSelectedVersion(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version slot kind: {LauncherCloudSyncEvidence.LastManualPullSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Manual Pull evidence selected version slot directory: {LauncherCloudSyncEvidence.LastManualPullSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Manual Pull completion flag recorded: {BoolText(LauncherCloudSyncEvidence.LastManualPullCompletionRecorded(dataDir))}");
        sb.AppendLine($"Manual Pull completed before Push: {BoolText(LauncherCloudSyncEvidence.LastManualPullBeforePushCompletionRecorded(dataDir))}");
        sb.AppendLine($"Manual Pull evidence is after branch switch: {BoolText(LauncherCloudSyncEvidence.LastManualPullIsAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Manual Pull evidence matches selected branch: {BoolText(LauncherCloudSyncEvidence.LastManualPullMatchesSelectedBranch(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Manual Pull completed after branch switch for selected version: {BoolText(LauncherCloudSyncEvidence.HasManualPullAfterBranchSwitch(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Current important Android local save evidence count: {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}");
        sb.AppendLine($"Current important Android local save evidence present: {BoolText(LauncherLocalSaveEvidence.HasImportantSaveEvidence(dataDir))}");
        sb.AppendLine($"Android save-origin marker filename: {LauncherSaveOriginEvidence.MarkerFileName}");
        sb.AppendLine($"Android save-origin marker path: {LauncherSaveOriginEvidence.MarkerPath(dataDir)}");
        sb.AppendLine($"Android save-origin marker present: {BoolText(LauncherSaveOriginEvidence.MarkerPresent(dataDir))}");
        sb.AppendLine($"Android save-origin UTC: {LauncherSaveOriginEvidence.OriginUtc(dataDir)}");
        sb.AppendLine($"Android save-origin UTC parseable: {BoolText(LauncherSaveOriginEvidence.OriginUtcParseable(dataDir))}");
        sb.AppendLine($"Android save-origin action: {LauncherSaveOriginEvidence.OriginAction(dataDir)}");
        sb.AppendLine($"Android save-origin selected branch: {LauncherSaveOriginEvidence.SelectedBranch(dataDir)}");
        sb.AppendLine($"Android save-origin selected version: {LauncherSaveOriginEvidence.SelectedVersion(dataDir)}");
        sb.AppendLine($"Android save-origin selected version slot kind: {LauncherSaveOriginEvidence.SelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Android save-origin selected version slot directory: {LauncherSaveOriginEvidence.SelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Android save-origin selected runtime slot ID: {LauncherSaveOriginEvidence.SelectedRuntimeSlotId(dataDir)}");
        sb.AppendLine($"Android save-origin selected PCK SHA256: {LauncherSaveOriginEvidence.SelectedPckSha256(dataDir)}");
        sb.AppendLine($"Android save-origin selected source sts2.dll SHA256: {LauncherSaveOriginEvidence.SelectedSourceAssemblySha256(dataDir)}");
        sb.AppendLine($"Android save-origin selected runtime playable at origin: {LauncherSaveOriginEvidence.SelectedRuntimePlayable(dataDir)}");
        sb.AppendLine($"Android save-origin selected runtime readiness problem at origin: {LauncherSaveOriginEvidence.SelectedRuntimeReadinessProblem(dataDir)}");
        sb.AppendLine($"Android save-origin important local save evidence count: {LauncherSaveOriginEvidence.ImportantLocalSaveEvidenceCount(dataDir)}");
        sb.AppendLine($"Android save-origin matches selected branch: {BoolText(LauncherSaveOriginEvidence.MatchesSelectedBranch(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Android save-origin current selected runtime is playable: {BoolText(LauncherSaveOriginEvidence.SelectedRuntimeCurrentlyPlayable(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Android save-origin selected runtime slot ID matches current runtime: {BoolText(LauncherSaveOriginEvidence.RuntimeSlotIdMatchesSelectedRuntime(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Android save-origin selected PCK matches current runtime: {BoolText(LauncherSaveOriginEvidence.PckMatchesSelectedRuntime(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Android save-origin selected source sts2.dll matches current runtime: {BoolText(LauncherSaveOriginEvidence.SourceAssemblyMatchesSelectedRuntime(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Android local saves verified for selected branch: {BoolText(LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedBranch(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Android local saves verified for selected runtime: {BoolText(LauncherSaveOriginEvidence.CurrentLocalSavesMatchSelectedRuntime(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Baseline manual Push prerequisites satisfied: {BoolText(LauncherCloudSyncEvidence.BaselineManualPushPrerequisitesSatisfied(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Manual Push evidence marker filename: {LauncherCloudSyncEvidence.LastManualPushMarkerFileName}");
        sb.AppendLine($"Manual Push evidence marker path: {LauncherCloudSyncEvidence.LastManualPushMarkerPath(dataDir)}");
        sb.AppendLine($"Manual Push evidence marker present: {BoolText(File.Exists(LauncherCloudSyncEvidence.LastManualPushMarkerPath(dataDir)))}");
        sb.AppendLine($"Latest manual Push evidence outcome: {LauncherCloudSyncEvidence.LatestManualPushEvidenceOutcome(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence UTC: {LauncherCloudSyncEvidence.LatestManualPushEvidenceUtc(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected branch: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedBranch(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected branch selection kind: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selector mode: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectorMode(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected version: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedVersion(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected version slot kind: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence selected version slot directory: {LauncherCloudSyncEvidence.LatestManualPushEvidenceSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Latest manual Push evidence reason: {LauncherCloudSyncEvidence.LatestManualPushEvidenceReason(dataDir)}");
        sb.AppendLine($"Manual Push evidence UTC: {LauncherCloudSyncEvidence.LastManualPushUtc(dataDir)}");
        sb.AppendLine($"Manual Push evidence UTC parseable: {BoolText(LauncherCloudSyncEvidence.LastManualPushUtcParseable(dataDir))}");
        sb.AppendLine($"Manual Push evidence selected branch: {LauncherCloudSyncEvidence.LastManualPushSelectedBranch(dataDir)}");
        sb.AppendLine($"Manual Push evidence selected branch selection kind: {LauncherCloudSyncEvidence.LastManualPushSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Manual Push evidence selector mode: {LauncherCloudSyncEvidence.LastManualPushSelectorMode(dataDir)}");
        sb.AppendLine($"Manual Push evidence selected version: {LauncherCloudSyncEvidence.LastManualPushSelectedVersion(dataDir)}");
        sb.AppendLine($"Manual Push evidence selected version slot kind: {LauncherCloudSyncEvidence.LastManualPushSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Manual Push evidence selected version slot directory: {LauncherCloudSyncEvidence.LastManualPushSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded local backup count: {LauncherCloudSyncEvidence.LastManualPushRecordedLocalBackupCount(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded cloud backup count: {LauncherCloudSyncEvidence.LastManualPushRecordedCloudBackupCount(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded latest local backup UTC: {LauncherCloudSyncEvidence.LastManualPushRecordedLatestLocalBackupUtc(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded latest cloud backup UTC: {LauncherCloudSyncEvidence.LastManualPushRecordedLatestCloudBackupUtc(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded important local save evidence count: {LauncherCloudSyncEvidence.LastManualPushRecordedImportantLocalSaveEvidenceCount(dataDir)}");
        sb.AppendLine($"Manual Push evidence recorded baseline prerequisites satisfied: {LauncherCloudSyncEvidence.LastManualPushRecordedBaselinePrerequisitesSatisfied(dataDir)}");
        sb.AppendLine($"Manual Push completion flag recorded: {BoolText(LauncherCloudSyncEvidence.LastManualPushCompletionRecorded(dataDir))}");
        sb.AppendLine($"Manual Push evidence is after branch switch: {BoolText(LauncherCloudSyncEvidence.LastManualPushIsAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Manual Push evidence matches selected branch: {BoolText(LauncherCloudSyncEvidence.LastManualPushMatchesSelectedBranch(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Manual Push evidence recorded pre-Push backup evidence satisfied: {BoolText(LauncherCloudSyncEvidence.LastManualPushPrePushBackupEvidenceSatisfied(dataDir))}");
        sb.AppendLine($"Manual Push completed after branch switch for selected version with backup evidence: {BoolText(LauncherCloudSyncEvidence.HasManualPushAfterBranchSwitch(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Manual Push blocked evidence marker filename: {LauncherCloudSyncEvidence.LastManualPushBlockedMarkerFileName}");
        sb.AppendLine($"Manual Push blocked evidence marker path: {LauncherCloudSyncEvidence.LastManualPushBlockedMarkerPath(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence marker present: {BoolText(File.Exists(LauncherCloudSyncEvidence.LastManualPushBlockedMarkerPath(dataDir)))}");
        sb.AppendLine($"Manual Push blocked evidence UTC: {LauncherCloudSyncEvidence.LastManualPushBlockedUtc(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence UTC parseable: {BoolText(LauncherCloudSyncEvidence.LastManualPushBlockedUtcParseable(dataDir))}");
        sb.AppendLine($"Manual Push blocked evidence selected branch: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedBranch(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selected branch selection kind: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedBranchSelectionKind(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selector mode: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectorMode(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selected version: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedVersion(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selected version slot kind: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedVersionSlotKind(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence selected version slot directory: {LauncherCloudSyncEvidence.LastManualPushBlockedSelectedVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence matches selected branch: {BoolText(LauncherCloudSyncEvidence.LastManualPushBlockedMatchesSelectedBranch(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Manual Push blocked evidence recorded prerequisites satisfied: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedPrerequisitesSatisfied(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded local backup count: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedLocalBackupCount(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded cloud backup count: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedCloudBackupCount(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded latest local backup UTC: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedLatestLocalBackupUtc(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded latest cloud backup UTC: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedLatestCloudBackupUtc(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded important local save evidence count: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedImportantLocalSaveEvidenceCount(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded baseline prerequisites satisfied: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedBaselinePrerequisitesSatisfied(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence recorded pre-Push backup evidence satisfied: {LauncherCloudSyncEvidence.LastManualPushBlockedRecordedPrePushBackupEvidenceSatisfied(dataDir)}");
        sb.AppendLine($"Manual Push blocked evidence reason: {LauncherCloudSyncEvidence.LastManualPushBlockedReason(dataDir)}");
        sb.AppendLine($"Manual Push blocked before upload evidence recorded: {BoolText(LauncherCloudSyncEvidence.LastManualPushBlockedBeforeUpload(dataDir))}");
        sb.AppendLine($"Important Android local save evidence count in bounded scan: {importantSaveEvidenceCount}");
        sb.AppendLine($"Important Android local save evidence present: {BoolText(importantSaveEvidenceCount > 0)}");
        sb.AppendLine($"Backup storage permission available: {BoolText(STS2Mobile.AppPaths.HasStoragePermission())}");
        sb.AppendLine($"Backup storage directory: {STS2Mobile.AppPaths.ExternalSaveBackupsDir}");
        sb.AppendLine($"Backup storage directory exists: {BoolText(Directory.Exists(STS2Mobile.AppPaths.ExternalSaveBackupsDir))}");
        sb.AppendLine($"Branch-switch manual Push prerequisites satisfied: {BoolText(LauncherBranchSwitchSafety.ManualPushPrerequisitesSatisfied(dataDir, LauncherPreferences.ReadGameBranch()))}");
        sb.AppendLine($"Pre-Push local backup evidence count: {LauncherBackupEvidence.LocalPrePushBackupCount()}");
        sb.AppendLine($"Pre-Push cloud backup evidence count: {LauncherBackupEvidence.CloudPrePushBackupCount()}");
        sb.AppendLine($"Latest pre-Push local backup UTC: {LauncherBackupEvidence.LatestLocalPrePushBackupUtc()}");
        sb.AppendLine($"Latest pre-Push cloud backup UTC: {LauncherBackupEvidence.LatestCloudPrePushBackupUtc()}");
        sb.AppendLine($"Pre-Push local backup evidence after branch switch: {BoolText(LauncherBackupEvidence.HasLocalPrePushBackupAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Pre-Push cloud backup evidence after branch switch: {BoolText(LauncherBackupEvidence.HasCloudPrePushBackupAfterBranchSwitch(dataDir))}");
        sb.AppendLine($"Branch-switch pre-Push backup evidence satisfied: {BoolText(LauncherBackupEvidence.HasPrePushBackupEvidenceAfterBranchSwitch(dataDir))}");
    }

    private static void AppendCachedGameVersions(StringBuilder sb, string dataDir)
    {
        var selectedBranch = SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch());
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        sb.AppendLine($"Current selected branch for version marker comparison: {selectedBranch}");
        sb.AppendLine($"Game version redownload marker filename: {LauncherGameFiles.RedownloadMarkerFileName}");
        sb.AppendLine($"Game version redownload marker path: {LauncherGameFiles.RedownloadMarkerPath(dataDir)}");
        sb.AppendLine($"Game version redownload marker present: {BoolText(File.Exists(LauncherGameFiles.RedownloadMarkerPath(dataDir)))}");
        sb.AppendLine($"Game version redownload marker UTC: {LauncherGameFiles.RedownloadMarkerUtc(dataDir)}");
        sb.AppendLine($"Game version redownload marker UTC parseable: {BoolText(LauncherGameFiles.RedownloadMarkerUtcParseable(dataDir))}");
        sb.AppendLine($"Game version redownload marker selected branch: {LauncherGameFiles.RedownloadMarkerSelectedBranch(dataDir)}");
        sb.AppendLine($"Game version redownload marker matches selected branch: {BoolText(MarkerBranchMatchesSelected(LauncherGameFiles.RedownloadMarkerSelectedBranch(dataDir), selectedBranch))}");
        sb.AppendLine($"Game version redownload marker selected version: {LauncherGameFiles.RedownloadMarkerSelectedVersion(dataDir)}");
        sb.AppendLine($"Game version redownload marker selected version slot kind: {LauncherGameFiles.RedownloadMarkerVersionSlotKind(dataDir)}");
        sb.AppendLine($"Game version redownload marker selected version slot directory: {LauncherGameFiles.RedownloadMarkerVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Game version redownload marker game directory: {LauncherGameFiles.RedownloadMarkerGameDirectory(dataDir)}");
        sb.AppendLine($"Game version redownload marker game directory existed before delete: {LauncherGameFiles.RedownloadMarkerGameDirectoryExisted(dataDir)}");
        sb.AppendLine($"Game version redownload marker game directory exists after delete: {LauncherGameFiles.RedownloadMarkerGameDirectoryExistsAfterDelete(dataDir)}");
        sb.AppendLine($"Game version redownload marker download state directory: {LauncherGameFiles.RedownloadMarkerDownloadStateDirectory(dataDir)}");
        sb.AppendLine($"Game version redownload marker download state directory existed before delete: {LauncherGameFiles.RedownloadMarkerDownloadStateDirectoryExisted(dataDir)}");
        sb.AppendLine($"Game version redownload marker download state directory exists after delete: {LauncherGameFiles.RedownloadMarkerDownloadStateDirectoryExistsAfterDelete(dataDir)}");
        sb.AppendLine($"Game version redownload marker selected directories cleared: {BoolText(LauncherGameFiles.RedownloadMarkerSelectedDirectoriesCleared(dataDir))}");
        sb.AppendLine($"Game version cache cleanup marker filename: {LauncherGameFiles.CacheCleanupMarkerFileName}");
        sb.AppendLine($"Game version cache cleanup marker path: {LauncherGameFiles.CacheCleanupMarkerPath(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker present: {BoolText(File.Exists(LauncherGameFiles.CacheCleanupMarkerPath(dataDir)))}");
        sb.AppendLine($"Game version cache cleanup marker UTC: {LauncherGameFiles.CacheCleanupMarkerUtc(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker UTC parseable: {BoolText(LauncherGameFiles.CacheCleanupMarkerUtcParseable(dataDir))}");
        sb.AppendLine($"Game version cache cleanup marker selected branch: {LauncherGameFiles.CacheCleanupMarkerSelectedBranch(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker matches selected branch: {BoolText(MarkerBranchMatchesSelected(LauncherGameFiles.CacheCleanupMarkerSelectedBranch(dataDir), selectedBranch))}");
        sb.AppendLine($"Game version cache cleanup marker selected version: {LauncherGameFiles.CacheCleanupMarkerSelectedVersion(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected version slot kind: {LauncherGameFiles.CacheCleanupMarkerVersionSlotKind(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected version slot directory: {LauncherGameFiles.CacheCleanupMarkerVersionSlotDirectory(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker game_versions present: {LauncherGameFiles.CacheCleanupMarkerGameVersionsDirectoryPresent(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker runtime_packs present: {LauncherGameFiles.CacheCleanupMarkerRuntimePacksDirectoryPresent(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected runtime pack directory: {LauncherGameFiles.CacheCleanupMarkerSelectedRuntimePackDirectory(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected runtime pack present before cleanup: {LauncherGameFiles.CacheCleanupMarkerSelectedRuntimePackPresentBeforeCleanup(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker removed count: {LauncherGameFiles.CacheCleanupMarkerRemovedCount(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker removed runtime pack count: {LauncherGameFiles.CacheCleanupMarkerRemovedRuntimePackCount(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected cache preserved where applicable: {BoolText(LauncherGameFiles.CacheCleanupMarkerSelectedCachePreservedWhereApplicable(dataDir))}");
        sb.AppendLine($"Game version cache cleanup marker selected runtime pack preserved where applicable: {BoolText(LauncherGameFiles.CacheCleanupMarkerSelectedRuntimePackPreservedWhereApplicable(dataDir))}");
        if (!Directory.Exists(versionsDir))
        {
            sb.AppendLine("Cached non-public game versions: 0");
            return;
        }

        var caches = LauncherGameVersionCache.Enumerate(dataDir, selectedBranch);
        sb.AppendLine($"Cached non-public game versions: {caches.Count}");
        foreach (var cache in caches)
        {
            var selected = cache.Selected ? "true" : "false";
            var inactive = !cache.Selected
                || string.Equals(selectedBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase);
            var markerPath = Path.Combine(
                cache.Path,
                SteamGameInstallPaths.LegacyPublicGameDirectory,
                SteamGameInstallPaths.BranchMarkerFileName
            );
            var markerBranch = ReadBranchMarkerBranch(markerPath);
            sb.AppendLine(
                $"Cached game version dir: {cache.DirectoryName} "
                    + $"selected={selected} "
                    + $"inactive={BoolText(inactive)} "
                    + $"branchMarkerPresent={BoolText(File.Exists(markerPath))} "
                    + $"branchMarkerBranch={markerBranch} "
                    + $"branchMarkerExpectedInstallSlotKind={SteamGameInstallPaths.VersionSlotKind(markerBranch)} "
                    + $"branchMarkerExpectedInstallSlotDirectory={cache.Path} "
                    + $"branchMarkerMatchingInstallSlotProvenance={BoolText(BranchMarkerHasInstallSlotProvenance(markerPath, SteamGameInstallPaths.VersionSlotKind(markerBranch), cache.Path))} "
                    + $"branchMarkerDepotManifests={BranchMarkerDepotManifestCount(markerPath)} "
                    + $"branchMarkerIntegrityProvenance={BoolText(BranchMarkerHasIntegrityProvenance(markerPath))} "
                    + $"branchMarkerDepotsMatchingPublic={ReadBranchMarkerValue(markerPath, "Depot manifests matching public count:")} "
                    + $"branchMarkerDepotsDifferingFromPublic={ReadBranchMarkerValue(markerPath, "Depot manifests differing from public count:")} "
                    + $"branchMarkerDepotsInheritedFromPublic={ReadBranchMarkerValue(markerPath, "Depot manifests inherited from public count:")} "
                    + $"branchMarkerDepotsMissingSelectedManifest={ReadBranchMarkerValue(markerPath, "Depot manifests missing selected branch manifest count:")} "
                    + $"branchMarkerReady={BoolText(CachedBranchMarkerReady(cache.DirectoryName, markerBranch, markerPath, cache.Path))}"
            );
        }
    }

    private static void AppendPreviousLaunchPhase(StringBuilder sb, string label)
    {
        var phase = LauncherLaunchMarkers.ReadStartupPhase();
        if (!string.IsNullOrWhiteSpace(phase))
            sb.AppendLine($"{label}: {phase}");
    }

    private static string ValueOrMissing(string value)
        => string.IsNullOrWhiteSpace(value) ? MissingDiagnosticValue : value;

    private static string BoolText(bool value)
        => value ? "true" : "false";

    private static bool MarkerBranchMatchesSelected(string markerBranch, string selectedBranch)
        => !string.IsNullOrWhiteSpace(markerBranch)
            && !markerBranch.StartsWith("<", System.StringComparison.Ordinal)
            && string.Equals(
                SteamGameBranch.Normalize(markerBranch),
                SteamGameBranch.Normalize(selectedBranch),
                System.StringComparison.OrdinalIgnoreCase
            );
}
