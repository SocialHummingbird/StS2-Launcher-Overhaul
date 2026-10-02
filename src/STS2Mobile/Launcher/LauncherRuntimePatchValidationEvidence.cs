using System.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;
internal static partial class LauncherRuntimePatchValidationEvidence
{
    internal const string MarkerFileName = "last_runtime_patch_validation.json";
    internal static string MarkerPath(string dataDir) => Path.Combine(dataDir, MarkerFileName);
    internal static bool MarkerPresent(string dataDir) => File.Exists(MarkerPath(dataDir));
    internal static Snapshot ReadSnapshot(string dataDir) => new(LauncherMarkerFile.ReadJsonSnapshot(MarkerPath(dataDir)));
    internal sealed class Snapshot
    {
        private readonly LauncherMarkerFile.JsonSnapshot _marker;
        internal Snapshot(LauncherMarkerFile.JsonSnapshot marker) => _marker = marker;
        internal bool Present => _marker.Present;
        internal string Status => _marker.ReadString("status");
        internal string Utc => _marker.ReadString("utc");
        internal string SelectedBranch => _marker.ReadString("selectedBranch");
        internal string SelectedVersion => _marker.ReadString("selectedVersion");
        internal string SelectedPckSha256 => _marker.ReadString("selectedPckSha256");
        internal string SelectedSourceAssemblySha256 => _marker.ReadString("selectedSourceAssemblySha256");
        internal string GameIdentityId => _marker.ReadString("gameIdentityId");
        internal string ActiveAndroidAssemblySha256 => _marker.ReadString("activeAndroidAssemblySha256");
        internal string RuntimePackId => _marker.ReadString("runtimePackId");
        internal string RuntimePackStatus => _marker.ReadString("runtimePackStatus");
        internal string AppliedPatchCount => _marker.ReadString("appliedPatchCount");
        internal string FailedPatchCount => _marker.ReadString("failedPatchCount");
        internal string TotalPatchCount => _marker.ReadString("totalPatchCount");
        internal bool UtcParseable => DateTime.TryParse(Utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out _);
        internal string FailureMessages => _marker.FailureMessages;
    }

    internal static void Write(string dataDir, STS2Mobile.StartupPatchOrchestrator.StartupPatchResult result)
    {
        try
        {
            var branch = LauncherPreferences.ReadGameBranch();
            var failures = result.FailureMessages().Take(20).ToArray();
            var status = result.CriticalFailed ? "critical_failed" : result.HasFailures ? "passed_with_noncritical_failures" : "passed";
            var slot = GameRuntimeSlot.Inspect(dataDir, branch);
            var payload = new
            {
                status,
                utc = DateTime.UtcNow.ToString("O"),
                selectedBranch = SteamGameBranch.Normalize(branch),
                selectedVersion = SteamGameBranch.DisplayName(branch),
                selectedVersionSlotKind = SteamGameInstallPaths.VersionSlotKind(branch),
                selectedVersionSlotDirectory = SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
                gameIdentityId = slot.GameIdentityId,
                selectedPckSha256 = slot.PckSha256,
                selectedSourceAssemblySha256 = slot.SourceAssemblySha256,
                activeAndroidAssemblySha256 = slot.ActiveAndroidAssemblySha256,
                runtimePackId = slot.RuntimePack?.PackId ?? string.Empty,
                runtimePackStatus = slot.RuntimePackUsabilityStatus,
                patchCompatibleBeforeLaunch = !result.CriticalFailed,
                runtimeCompatibleBeforeLaunch = slot.RuntimeCompatible,
                playableBeforeLaunch = slot.Playable && !result.CriticalFailed,
                criticalFailed = result.CriticalFailed,
                hasFailures = result.HasFailures,
                appliedPatchCount = result.AppliedPatchCount,
                failedPatchCount = result.FailedPatchCount,
                totalPatchCount = result.TotalPatchCount,
                durationMs = result.Duration.TotalMilliseconds,
                failureMessages = failures
            };
            File.WriteAllText(MarkerPath(dataDir), JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write runtime patch validation evidence: {ex.Message}");
        }
    }
}
