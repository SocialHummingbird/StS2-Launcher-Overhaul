using System.Text;
using System;
using System.IO;
using STS2Mobile.Steam;
using System.Collections.Generic;
using System.Linq;
using STS2Mobile.Steam.Workshop;
using STS2Mobile;

namespace STS2Mobile.Launcher;
internal static partial class LauncherDiagnostics
{
    private const string MissingDiagnosticValue = "<none>";
    internal sealed partial class Snapshot
    {
        internal Snapshot(string dataDir, string accountName, bool hasSavedCredentials, LauncherLaunchReadiness launchReadiness, string sessionState, string failReason)
        {
            _state = new LauncherStateReport(dataDir, accountName, hasSavedCredentials, launchReadiness, sessionState, failReason);
        }

        private readonly LauncherStateReport _state;
        internal string WriteDiagnosticsReport() => CreateTimestampedText("StS2 Launcher diagnostics", GeneratedUtcLabel, AppendFullLauncherDiagnostics).Write("sts2-mobile-diagnostics", _state.DataDir);
        private void AppendFullLauncherDiagnostics(StringBuilder sb)
        {
            AppendPublicSharingWarning(sb);
            AppendLauncherState(sb, LauncherStateDetail.Detailed);
            AppendLauncherPreferences(sb, _state.DataDir, _state.LaunchReadiness);
            AppendFullReportDiagnostics(sb, _state.DataDir);
        }

        private const string ErrorReportGeneratedAtLabel = "UTC";
        private const string PreviousLaunchPhaseLabel = "Previous launch phase";
        private readonly struct ErrorReportDefinition
        {
            private ErrorReportDefinition(string title, LauncherStateDetail stateDetail, Action<StringBuilder> appendDiagnostics, string footer)
            {
                Title = title;
                StateDetail = stateDetail;
                AppendDiagnostics = appendDiagnostics;
                Footer = footer;
            }

            internal string Title { get; }
            internal LauncherStateDetail StateDetail { get; }
            internal Action<StringBuilder> AppendDiagnostics { get; }
            internal string Footer { get; }

            internal static ErrorReportDefinition Summary(string dataDir) => new("=== LAST ERROR SUMMARY ===", LauncherStateDetail.Compact, sb => AppendSummaryErrorDiagnostics(sb, dataDir), "=== END LAST ERROR SUMMARY ===");
            internal static ErrorReportDefinition RawLog(string dataDir) => new("=== RAW ERROR LOG ===", LauncherStateDetail.Detailed, sb => AppendRawErrorDiagnostics(sb, dataDir), "=== END RAW ERROR LOG ===");
        }

        internal string BuildDiagnosticsSummary() => BuildErrorReport(ErrorReportDefinition.Summary(_state.DataDir));
        internal string BuildRawErrorLog() => BuildErrorReport(ErrorReportDefinition.RawLog(_state.DataDir));
        private void AppendLauncherState(StringBuilder sb, LauncherStateDetail detail) => _state.AppendTo(sb, detail);
        private string BuildErrorReport(ErrorReportDefinition report) => CreateTimestampedText(report.Title, ErrorReportGeneratedAtLabel, sb =>
        {
            AppendLauncherState(sb, report.StateDetail);
            AppendPreviousLaunchPhase(sb, PreviousLaunchPhaseLabel);
            report.AppendDiagnostics(sb);
            sb.AppendLine(report.Footer);
        }).Build();
        private enum LauncherStateDetail
        {
            Compact,
            Detailed,
        }

        private readonly struct LauncherStateReport
        {
            internal LauncherStateReport(string dataDir, string accountName, bool hasSavedCredentials, LauncherLaunchReadiness launchReadiness, string sessionState, string failReason)
            {
                DataDir = dataDir;
                AccountName = accountName;
                HasSavedCredentials = hasSavedCredentials;
                LaunchReadiness = launchReadiness;
                SessionState = sessionState;
                FailReason = failReason;
            }

            internal string DataDir { get; }
            internal LauncherLaunchReadiness LaunchReadiness { get; }
            private string AccountName { get; }
            private bool HasSavedCredentials { get; }
            private string SessionState { get; }
            private string FailReason { get; }

            internal void AppendTo(StringBuilder sb, LauncherStateDetail detail)
            {
                if (detail == LauncherStateDetail.Detailed)
                    AppendDetailedPrefix(sb);
                AppendCompact(sb);
                if (detail == LauncherStateDetail.Detailed)
                    sb.AppendLine($"Fail reason: {ValueOrMissing(FailReason)}");
            }

            private void AppendDetailedPrefix(StringBuilder sb)
            {
                sb.AppendLine($"Data dir: {ValueOrMissing(DataDir)}");
                sb.AppendLine($"Account: {ValueOrMissing(AccountName)}");
                sb.AppendLine($"Has saved credentials: {HasSavedCredentials}");
            }

            private void AppendCompact(StringBuilder sb)
            {
                sb.AppendLine($"Game files ready: {BoolText(LaunchReadiness?.Ready == true)}");
                sb.AppendLine($"Launch readiness cache status: {ValueOrMissing(LaunchReadiness?.CacheStatus)}");
                sb.AppendLine($"Session state: {ValueOrMissing(SessionState)}");
            }
        }
    }

    private static void AppendPublicSharingWarning(StringBuilder sb)
    {
        sb.AppendLine("Public sharing warning: review and redact this diagnostics report before posting publicly.");
        sb.AppendLine("It may include account names, local paths, device details, launcher state, and log excerpts.");
        sb.AppendLine();
    }

    internal static void WriteMainMenuPreparation(string classification, params string[] details)
    {
        var evidence = MainMenuPreparation(Godot.OS.GetDataDir());
        try
        {
            evidence.WriteAllText(CreateTimestampedText("StS2 Launcher main-menu preparation", "UTC", sb =>
            {
                sb.AppendLine($"Classification: {CleanPostStartupValue(classification)}");
                if (details == null)
                    return;
                foreach (var detail in details)
                    sb.AppendLine(CleanPostStartupValue(detail));
            }).Build());
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Main-menu preparation evidence failed: {ex.Message}");
        }
    }

    private static void AppendCachedGameVersions(StringBuilder sb, string dataDir)
    {
        var selectedBranch = SteamGameBranch.Normalize(LauncherPreferences.ReadGameBranch());
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        sb.AppendLine($"Current selected branch for version marker comparison: {selectedBranch}");
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
            var inactive = !cache.Selected || string.Equals(selectedBranch, SteamGameBranch.Public, System.StringComparison.OrdinalIgnoreCase);
            var markerPath = Path.Combine(cache.Path, SteamGameInstallPaths.LegacyPublicGameDirectory, SteamGameInstallPaths.BranchMarkerFileName);
            var markerBranch = ReadBranchMarkerBranch(markerPath);
            sb.AppendLine($"Cached game version dir: {cache.DirectoryName} " + $"selected={selected} " + $"inactive={BoolText(inactive)} " + $"branchMarkerPresent={BoolText(File.Exists(markerPath))} " + $"branchMarkerBranch={markerBranch} " + $"branchMarkerExpectedInstallSlotKind={SteamGameInstallPaths.VersionSlotKind(markerBranch)} " + $"branchMarkerExpectedInstallSlotDirectory={cache.Path} " + $"branchMarkerMatchingInstallSlotProvenance={BoolText(BranchMarkerHasInstallSlotProvenance(markerPath, SteamGameInstallPaths.VersionSlotKind(markerBranch), cache.Path))} " + $"branchMarkerDepotManifests={BranchMarkerDepotManifestCount(markerPath)} " + $"branchMarkerIntegrityProvenance={BoolText(BranchMarkerHasIntegrityProvenance(markerPath))} " + $"branchMarkerDepotsMatchingPublic={ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.DepotsMatchingPublic)} " + $"branchMarkerDepotsDifferingFromPublic={ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.DepotsDifferingFromPublic)} " + $"branchMarkerDepotsInheritedFromPublic={ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.DepotsInheritedFromPublic)} " + $"branchMarkerDepotsMissingSelectedManifest={ReadBranchMarkerValue(markerPath, LauncherBranchMarkerFields.DepotsMissingSelectedManifest)} " + $"branchMarkerReady={BoolText(CachedBranchMarkerReady(cache.DirectoryName, markerBranch, markerPath, cache.Path))}");
        }
    }

    private static void AppendFullReportDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendDiagnosticReportFiles(sb, dataDir);
        AppendAndroidBridgeSection(sb, dataDir);
    }

    private static void AppendDiagnosticReportFiles(StringBuilder sb, string dataDir)
    {
        AppendFileSummaries(sb, DiagnosticReportFiles(dataDir), inlineContentLimit: 4096);
        foreach (var directory in DiagnosticReportDirectories(dataDir))
            AppendDirectoryListing(sb, directory);
    }

    private static IEnumerable<DiagnosticFile> DiagnosticReportFiles(string dataDir)
    {
        foreach (var file in StartupStateFiles(dataDir))
            yield return file;
        yield return SteamAuthFailure(dataDir);
        yield return ShaderWarmupStatus(dataDir);
        yield return LaunchAttempt(dataDir);
        yield return ManualSafeLaunchMarker(dataDir);
        yield return new DiagnosticFile("Selected game branch marker", STS2Mobile.Steam.SteamGameInstallPaths.BranchMarkerPath(dataDir, LauncherPreferences.ReadGameBranch()));
        yield return new DiagnosticFile("Workshop sync manifest", AppPaths.WorkshopManifestPath(dataDir));
        yield return new DiagnosticFile("Workshop clear marker", AppPaths.WorkshopClearMarkerPath(dataDir));
        yield return BootstrapTrace();
        yield return GamePck(dataDir);
    }

    private static IEnumerable<DiagnosticFile> StartupStateFiles(string dataDir)
    {
        yield return StartupMarker(dataDir);
        yield return StartupContext(dataDir);
        yield return MainMenuPreparation(dataDir);
        yield return PostStartupHeartbeat(dataDir);
        yield return PostStartupTrace(dataDir);
        yield return RendererAttempt(dataDir);
        yield return ProcessExitInfo(dataDir);
        yield return AppLifecycle(dataDir);
        yield return StartupTimeline(dataDir);
        yield return StartupSceneSnapshot(dataDir);
    }

    private static IEnumerable<DiagnosticDirectory> DiagnosticReportDirectories(string dataDir)
    {
        yield return new DiagnosticDirectory("Game directory", LauncherGameFiles.GameDirectoryPath(dataDir, LauncherPreferences.ReadGameBranch()), 2);
        yield return new DiagnosticDirectory("Game versions", Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory), 2);
        yield return new DiagnosticDirectory("Download state", STS2Mobile.Steam.SteamGameInstallPaths.DownloadStateDirectoryPath(dataDir, LauncherPreferences.ReadGameBranch()), 1);
        yield return new DiagnosticDirectory("Workshop staged mods", AppPaths.WorkshopStagedModsDir(dataDir), 2);
        yield return new DiagnosticDirectory("Workshop downloads", AppPaths.WorkshopDownloadsDir(dataDir), 2);
        yield return new DiagnosticDirectory("Mono publish root", Path.Combine(dataDir, LauncherStorageNames.GodotDirectory, LauncherStorageNames.MonoDirectory, LauncherStorageNames.PublishDirectory), 2);
    }

    private static void AppendLauncherPreferences(StringBuilder sb, string dataDir, LauncherLaunchReadiness launchReadiness)
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

    private static void AppendPreviousLaunchPhase(StringBuilder sb, string label)
    {
        var phase = LauncherLaunchMarkers.ReadStartupPhase();
        if (!string.IsNullOrWhiteSpace(phase))
            sb.AppendLine($"{label}: {phase}");
    }

    private static string ValueOrMissing(string value) => string.IsNullOrWhiteSpace(value) ? MissingDiagnosticValue : value;
    private static string BoolText(bool value) => value ? "true" : "false";
    private static void AppendWorkshopDiagnostics(StringBuilder sb, string dataDir)
    {
        var downloadsDirectory = AppPaths.WorkshopDownloadsDir(dataDir);
        var stagedDirectory = AppPaths.WorkshopStagedModsDir(dataDir);
        var manifestPath = AppPaths.WorkshopManifestPath(dataDir);
        var clearMarkerPath = AppPaths.WorkshopClearMarkerPath(dataDir);
        var manifest = new SteamWorkshopStager(downloadsDirectory, stagedDirectory, manifestPath).LoadManifest();
        sb.AppendLine($"Workshop sync manifest path: {manifestPath}");
        sb.AppendLine($"Workshop sync manifest present: {BoolText(File.Exists(manifestPath))}");
        sb.AppendLine($"Workshop sync manifest generated UTC: {ValueOrMissing(manifest.GeneratedAtUtc)}");
        sb.AppendLine($"Workshop clear marker path: {clearMarkerPath}");
        sb.AppendLine($"Workshop clear marker present: {BoolText(File.Exists(clearMarkerPath))}");
        sb.AppendLine($"Workshop cleared UTC: {ValueOrMissing(manifest.ClearedAtUtc)}");
        sb.AppendLine($"Workshop clear reason: {ValueOrMissing(manifest.ClearReason)}");
        sb.AppendLine($"Workshop downloads directory: {downloadsDirectory}");
        sb.AppendLine($"Workshop downloads directory present: {BoolText(Directory.Exists(downloadsDirectory))}");
        sb.AppendLine($"Workshop staged mods directory: {stagedDirectory}");
        sb.AppendLine($"Workshop staged mods directory present: {BoolText(Directory.Exists(stagedDirectory))}");
        sb.AppendLine($"Workshop subscription query type: {ValueOrMissing(manifest.SubscriptionQueryType)}");
        sb.AppendLine($"Workshop subscription query attempts: {ValueOrMissing(manifest.SubscriptionQueryAttempts)}");
        sb.AppendLine($"Workshop subscribed item count: {manifest.SubscribedItemCount}");
        sb.AppendLine($"Workshop dependency item count: {manifest.DependencyItemCount}");
        sb.AppendLine($"Workshop missing dependency item count: {manifest.MissingDependencyItemCount}");
        sb.AppendLine($"Workshop missing dependency ids: {IdList(manifest.MissingDependencyIds)}");
        sb.AppendLine($"Workshop total discovered item count: {manifest.TotalItemCount}");
        sb.AppendLine($"Workshop manifest item count: {manifest.Items.Count}");
        sb.AppendLine($"Workshop active staged PCK mod count: {manifest.Items.Count(IsActiveWorkshopMod)}");
        sb.AppendLine($"Workshop staged no-PCK item count: {manifest.Items.Count(item => HasStatus(item, "staged-no-pck"))}");
        sb.AppendLine($"Workshop unsupported item count: {manifest.Items.Count(item => HasStatus(item, "unsupported"))}");
        sb.AppendLine($"Workshop failed item count: {manifest.Items.Count(IsFailedWorkshopItem)}");
        sb.AppendLine($"Workshop raw staged directory count: {DirectoryCount(stagedDirectory)}");
        sb.AppendLine($"Workshop raw staged PCK file count: {RawStagedPckCount(stagedDirectory)}");
        var moddedMode = LauncherModSelectionState.IsModdedMode;
        var knownMods = LauncherModSelectionState.KnownMods();
        var enabledMods = moddedMode ? knownMods.Where(mod => mod.Enabled && !mod.IsUnsupported).ToArray() : Array.Empty<LauncherKnownMod>();
        sb.AppendLine($"Mod selector play mode: {(moddedMode ? "modded" : "vanilla")}");
        sb.AppendLine($"Mod selector installed mod count: {knownMods.Count(mod => !mod.IsUnsupported)}");
        sb.AppendLine($"Mod selector enabled mod count: {enabledMods.Length}");
        foreach (var mod in enabledMods.Take(32))
        {
            sb.AppendLine("Mod selector enabled item: " + $"key={mod.Key} " + $"id={mod.Id} " + $"title=\"{mod.Title}\" " + $"source=\"{mod.Source}\" " + $"dependency={BoolText(mod.IsDependency)} " + $"requiredDependency={BoolText(mod.IsRequiredDependency)} " + $"hasPck={BoolText(mod.HasPck)} " + $"path=\"{mod.Path}\" " + $"pathPresent={BoolText(Directory.Exists(mod.Path))} " + $"jsonFiles={FileCount(mod.Path, "*.json")} " + $"pckFiles={FileCount(mod.Path, "*.pck")} " + $"dllFiles={FileCount(mod.Path, "*.dll")}");
        }

        foreach (var item in manifest.Items.Take(32))
        {
            sb.AppendLine("Workshop item: " + $"id={item.PublishedFileId} " + $"title=\"{item.Title}\" " + $"status={ValueOrMissing(item.Status)} " + $"hasPck={BoolText(item.HasPck)} " + $"files={item.FileCount} " + $"hash={ValueOrMissing(item.ContentSha256)} " + $"manifest={item.ManifestId} " + $"downloadSource={ValueOrMissing(item.DownloadSourceKind)} " + $"expectedBytes={item.ExpectedDownloadBytes} " + $"hcontent={item.HContentFile} " + $"reusedCache={BoolText(item.ReusedCachedDownload)} " + $"downloadUrlPresent={BoolText(item.DownloadUrlPresent)} " + $"downloadUrlHost={ValueOrMissing(item.DownloadUrlHost)} " + $"dependency={BoolText(item.IsDependency)} " + $"requiredBy=\"{IdList(item.RequiredByPublishedFileIds)}\" " + $"staged=\"{item.StagedDirectory}\" " + $"source=\"{item.SourceDirectory}\" " + $"error=\"{item.Error}\"");
        }
    }

    private static bool IsActiveWorkshopMod(SteamWorkshopSyncManifestItem item) => item.HasPck && HasStatus(item, "staged");
    private static bool HasStatus(SteamWorkshopSyncManifestItem item, string status) => string.Equals(item.Status, status, StringComparison.OrdinalIgnoreCase);
    private static bool IsFailedWorkshopItem(SteamWorkshopSyncManifestItem item) => !string.IsNullOrWhiteSpace(item.Status) && item.Status.EndsWith("failed", StringComparison.OrdinalIgnoreCase);
    private static string IdList(System.Collections.Generic.IEnumerable<ulong> ids) => ids == null ? "" : string.Join(",", ids.Where(id => id != 0).OrderBy(id => id));
    private static int DirectoryCount(string directory)
    {
        try
        {
            return Directory.Exists(directory) ? Directory.EnumerateDirectories(directory).Count() : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static int RawStagedPckCount(string stagedDirectory)
    {
        try
        {
            return Directory.Exists(stagedDirectory) ? Directory.EnumerateFiles(stagedDirectory, "*.pck", SearchOption.AllDirectories).Count() : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static int FileCount(string directory, string pattern)
    {
        try
        {
            return Directory.Exists(directory) ? Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories).Take(128).Count() : 0;
        }
        catch
        {
            return 0;
        }
    }

    private const string StartupRecoveryDiagnosticsTitle = "STS2 startup recovery diagnostics";
    internal static string WriteStartupRecoveryDiagnosticsReport(string dataDir) => StartupRecoveryDiagnostics(dataDir).Write("sts2-startup-recovery-diagnostics", dataDir);
    internal static string BuildStartupRecoveryDiagnosticsText(string dataDir) => StartupRecoveryDiagnostics(dataDir).Build();
    private static TimestampedText StartupRecoveryDiagnostics(string dataDir) => CreateTimestampedText(StartupRecoveryDiagnosticsTitle, GeneratedUtcLabel, sb => AppendStartupRecoveryDiagnostics(sb, dataDir));
    private static void AppendStartupRecoveryDiagnostics(StringBuilder sb, string dataDir)
    {
        AppendPublicSharingWarning(sb);
        sb.AppendLine($"Data dir: {dataDir}");
        sb.AppendLine();
        AppendFileContentsSections(sb, StartupRecoveryFiles(dataDir));
        AppendLogcatTail(sb, AndroidLogcatTail, StartupRecoveryTailLines);
    }

    private static IEnumerable<DiagnosticFile> StartupRecoveryFiles(string dataDir)
    {
        foreach (var file in StartupStateFiles(dataDir))
            yield return file;
        yield return BootstrapTrace();
    }

    private const string DiagnosticsDirectory = "diagnostics";
    private const string GeneratedUtcLabel = "Generated UTC";
    private readonly struct TimestampedText
    {
        internal TimestampedText(string title, string generatedAtLabel, Action<StringBuilder> appendBody)
        {
            Title = title;
            GeneratedAtLabel = generatedAtLabel;
            AppendBody = appendBody;
        }

        private string Title { get; }
        private string GeneratedAtLabel { get; }
        private Action<StringBuilder> AppendBody { get; }

        internal string Build() => BuildTimestampedText(Title, GeneratedAtLabel, AppendBody);
        internal string Write(string fileNamePrefix, string fallbackDirectory) => WriteTimestampedReport(fileNamePrefix, fallbackDirectory, Build());
    }

    private static string WriteTimestampedReport(string fileNamePrefix, string fallbackDirectory, string text)
    {
        var fileName = $"{fileNamePrefix}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.txt";
        var targetPath = TryGetExternalDiagnosticsPath(fileName) ?? Path.Combine(fallbackDirectory, fileName);
        var parent = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
        System.IO.File.WriteAllText(targetPath, text);
        return targetPath;
    }

    private static TimestampedText CreateTimestampedText(string title, string generatedAtLabel, Action<StringBuilder> appendBody) => new(title, generatedAtLabel, appendBody);
    private static string BuildTimestampedText(string title, string generatedAtLabel, Action<StringBuilder> appendBody)
    {
        var sb = new StringBuilder();
        sb.AppendLine(title);
        sb.AppendLine($"{generatedAtLabel}: {DateTime.UtcNow:O}");
        appendBody(sb);
        return sb.ToString();
    }

    private static string TryGetExternalDiagnosticsPath(string fileName)
    {
        if (!OperatingSystem.IsAndroid())
            return null;
        try
        {
            var externalDir = AndroidGodotAppBridge.GetExternalFilesDirPath();
            if (string.IsNullOrWhiteSpace(externalDir))
                return null;
            var diagnosticsDir = Path.Combine(externalDir, DiagnosticsDirectory);
            Directory.CreateDirectory(diagnosticsDir);
            return Path.Combine(diagnosticsDir, fileName);
        }
        catch
        {
            return null;
        }
    }
}
