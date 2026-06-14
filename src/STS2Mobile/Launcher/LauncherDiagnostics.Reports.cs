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
        sb.AppendLine($"Android credential Autofill provider model: {LauncherAutofillSupport.ProviderModel}");
        sb.AppendLine($"Godot login field Autofill hints configured: {BoolText(LauncherAutofillSupport.GodotFieldAutofillHintsConfigured)}");
        sb.AppendLine($"Native Android Autofill overlay supported: {BoolText(LauncherAutofillSupport.NativeAndroidAutofillOverlaySupported)}");
        sb.AppendLine($"Launcher stores Steam password for Autofill: {BoolText(LauncherAutofillSupport.AppStoresSteamPassword)}");
        sb.AppendLine($"Native Android Autofill result TTL seconds: {LauncherAutofillSupport.NativeDialogResultTtlSeconds}");
        sb.AppendLine($"Android credential Autofill implementation note: {LauncherAutofillSupport.CurrentImplementation}");
        sb.AppendLine($"SteamKit debug logs opt-in enabled: {BoolText(SteamConnectionConfigurationFactory.SteamKitDebugLogsOptInEnabled)}");
        sb.AppendLine($"SteamKit debug logs sanitized for credentials/tokens: {BoolText(SteamConnectionConfigurationFactory.SteamKitDebugLogsSanitized)}");
        sb.AppendLine($"Selected game branch storage directory: {SteamGameBranch.StateDirectoryName(branch)}");
        sb.AppendLine($"Selected game version slot kind: {SteamGameInstallPaths.VersionSlotKind(branch)}");
        sb.AppendLine($"Selected game version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch)}");
        sb.AppendLine($"Selected game directory: {SteamGameInstallPaths.GameDirectory(dataDir, branch)}");
        sb.AppendLine($"Selected game PCK: {LauncherGameFiles.PckPath(dataDir, branch)}");
        sb.AppendLine($"Selected game files ready: {BoolText(LauncherGameFiles.Ready(dataDir, branch))}");
        sb.AppendLine($"Selected game readiness problem: {ValueOrMissing(LauncherGameFiles.ReadinessProblem(dataDir, branch))}");
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
        sb.AppendLine($"Game version cache cleanup marker removed count: {LauncherGameFiles.CacheCleanupMarkerRemovedCount(dataDir)}");
        sb.AppendLine($"Game version cache cleanup marker selected cache preserved where applicable: {BoolText(LauncherGameFiles.CacheCleanupMarkerSelectedCachePreservedWhereApplicable(dataDir))}");
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
