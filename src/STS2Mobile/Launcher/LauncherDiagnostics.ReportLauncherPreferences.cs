using System.IO;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendLauncherPreferences(
        StringBuilder sb,
        string dataDir,
        LauncherLaunchReadiness launchReadiness
    )
    {
        var preferences = LauncherPreferences.ReadActionPreferences();
        var branch = SteamGameBranch.Normalize(preferences.GameBranch);
        sb.AppendLine($"Renderer pref: {preferences.RendererMode}");
        sb.AppendLine($"Renderer pref display name: {LauncherRendererMode.DisplayName(preferences.RendererMode)}");
        sb.AppendLine($"Renderer preference key: {LauncherStorageNames.RendererMode}");
        sb.AppendLine("Safe Start renderer policy: OpenGL on PowerVR; project renderer otherwise");
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
        sb.AppendLine($"SteamKit debug logs opt-in enabled: {BoolText(SteamConnectionConfigurationFactory.SteamKitDebugLogsOptInEnabled)}");
        sb.AppendLine($"SteamKit debug logs sanitized for credentials/tokens: {BoolText(SteamConnectionConfigurationFactory.SteamKitDebugLogsSanitized)}");
        sb.AppendLine($"Selected game branch storage directory: {SteamGameBranch.StateDirectoryName(branch)}");
        sb.AppendLine($"Selected game version slot kind: {SteamGameInstallPaths.VersionSlotKind(branch)}");
        sb.AppendLine($"Selected game version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch)}");
        sb.AppendLine($"Selected game directory: {SteamGameInstallPaths.GameDirectory(dataDir, branch)}");
        sb.AppendLine($"Selected game PCK: {LauncherGameFiles.PckPath(dataDir, branch)}");
        sb.AppendLine($"Selected game files ready: {BoolText(launchReadiness?.Ready == true)}");
        sb.AppendLine($"Selected game readiness problem: {ValueOrMissing(launchReadiness?.ReadinessProblem)}");
        sb.AppendLine($"Selected game readiness cache status: {ValueOrMissing(launchReadiness?.CacheStatus)}");
        sb.AppendLine($"Selected game readiness phase: {ValueOrMissing(launchReadiness?.EvaluationPhase)}");
        AppendGameRuntimeSlot(sb, dataDir, branch, launchReadiness);
        AppendWorkshopDiagnostics(sb, dataDir);
        sb.AppendLine($"Selected download state: {SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, branch)}");
        AppendBranchAvailability(sb, dataDir);
        var branchMarkerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, branch);
        sb.AppendLine($"Selected game branch marker: {branchMarkerPath}");
        sb.AppendLine($"Selected game branch marker present: {BoolText(File.Exists(branchMarkerPath))}");
        sb.AppendLine($"Selected game branch marker branch: {ReadBranchMarkerBranch(branchMarkerPath)}");
        sb.AppendLine($"Selected game branch marker install slot kind: {ReadBranchMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.InstallSlotKind)}");
        sb.AppendLine($"Selected game branch marker install slot directory: {ReadBranchMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.InstallSlotDirectory)}");
        sb.AppendLine($"Selected game branch marker expected install slot kind: {SteamGameInstallPaths.VersionSlotKind(branch)}");
        sb.AppendLine($"Selected game branch marker expected install slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch)}");
        sb.AppendLine($"Selected game branch marker has matching install slot provenance: {BoolText(BranchMarkerHasInstallSlotProvenance(branchMarkerPath, SteamGameInstallPaths.VersionSlotKind(branch), SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch)))}");
        sb.AppendLine($"Selected game branch marker has depot manifests: {BoolText(BranchMarkerHasDepotManifestProvenance(branchMarkerPath))}");
        sb.AppendLine($"Selected game branch marker has branch integrity provenance: {BoolText(BranchMarkerHasIntegrityProvenance(branchMarkerPath))}");
        sb.AppendLine($"Selected game branch marker depot manifest entries: {BranchMarkerDepotManifestCount(branchMarkerPath)}");
        sb.AppendLine($"Selected game branch marker depots matching public: {ReadBranchMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsMatchingPublic)}");
        sb.AppendLine($"Selected game branch marker depots differing from public: {ReadBranchMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsDifferingFromPublic)}");
        sb.AppendLine($"Selected game branch marker depots without public comparison: {ReadBranchMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsWithoutPublicComparison)}");
        sb.AppendLine($"Selected game branch marker depots inherited from public: {ReadBranchMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsInheritedFromPublic)}");
        sb.AppendLine($"Selected game branch marker depots missing selected branch manifest: {ReadBranchMarkerValue(branchMarkerPath, LauncherBranchMarkerFields.DepotsMissingSelectedManifest)}");
        sb.AppendLine($"Selected game branch marker partial Steam branch evidence: {BranchMarkerPartialSteamBranchEvidence(branchMarkerPath)}");
        sb.AppendLine($"Selected game branch marker depot manifest rows: {ReadBranchMarkerValues(branchMarkerPath, LauncherBranchMarkerFields.DepotManifestRow, 32)}");
        sb.AppendLine($"Selected game branch marker ready: {BoolText(LauncherGameFiles.BranchMarkerReady(dataDir, branch))}");
        AppendCachedGameVersions(sb, dataDir);
    }
}
