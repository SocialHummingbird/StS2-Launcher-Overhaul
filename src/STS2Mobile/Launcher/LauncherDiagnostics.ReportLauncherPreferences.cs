using System.IO;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendLauncherPortalUxFeatureReports(StringBuilder sb)
    {
        foreach (var feature in LauncherPortalUxSupport.FeatureReports)
            sb.AppendLine($"{feature.DiagnosticLabel}: {BoolText(feature.Supported)}");
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
        sb.AppendLine($"Native credential panel large field targets supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelLargeFieldTargetsSupported)}");
        sb.AppendLine($"Native credential panel requests both Autofill fields: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelRequestsBothAutofillFields)}");
        sb.AppendLine($"Native credential panel focus Autofill requests supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelFocusAutofillRequestsSupported)}");
        sb.AppendLine($"Native credential panel task-led buttons supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelTaskLedButtonsSupported)}");
        sb.AppendLine($"Native credential panel responsive action rows supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelResponsiveActionRowsSupported)}");
        sb.AppendLine($"Native credential panel orientation reflow supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelOrientationReflowSupported)}");
        sb.AppendLine($"Native credential panel short-height copy supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelShortHeightCopySupported)}");
        sb.AppendLine($"Native credential panel short-height reflow supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelShortHeightReflowSupported)}");
        sb.AppendLine($"Native credential panel IME height reflow supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelImeHeightReflowSupported)}");
        sb.AppendLine($"Native credential panel password visibility toggle supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelPasswordVisibilityToggleSupported)}");
        sb.AppendLine($"Native credential panel password-focus button supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelPasswordFocusButtonSupported)}");
        sb.AppendLine($"Native credential panel Back dismiss supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelBackDismissSupported)}");
        sb.AppendLine($"Native credential panel dismiss retry supported: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelDismissRetrySupported)}");
        sb.AppendLine($"Native credential panel dismiss hides keyboard: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelDismissHidesKeyboardSupported)}");
        sb.AppendLine($"Native credential panel suppresses pre-auth save prompt: {BoolText(LauncherCredentialEntrySupport.NativeCredentialPanelSuppressesPreAuthSavePrompt)}");
        sb.AppendLine($"Steam Guard one-shot code guidance supported: {BoolText(LauncherCredentialEntrySupport.SteamGuardOneShotCodeGuidanceSupported)}");
        sb.AppendLine($"Steam Guard alphanumeric keyboard supported: {BoolText(LauncherCredentialEntrySupport.SteamGuardAlphanumericKeyboardSupported)}");
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
        AppendLauncherPortalUxFeatureReports(sb);
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
}