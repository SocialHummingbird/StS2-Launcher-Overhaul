using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimePatchValidationEvidence
{
    internal static void Write(string dataDir, STS2Mobile.StartupPatchOrchestrator.StartupPatchResult result)
    {
        try
        {
            var branch = LauncherPreferences.ReadGameBranch();
            var failures = result.FailureMessages().Take(20).ToArray();
            var status = result.CriticalFailed
                ? "critical_failed"
                : result.HasFailures
                    ? "passed_with_noncritical_failures"
                    : "passed";
            var runtimeId = RuntimeCacheValue(dataDir, "Runtime ID:");
            var selectedPckSha256 = RuntimeCacheValue(dataDir, "Selected PCK SHA256:");
            var selectedSourceAssemblySha256 = RuntimeCacheValue(dataDir, "Selected source sts2.dll SHA256:");
            var activeAndroidAssemblySha256 = RuntimeCacheValue(dataDir, "Active source sts2.dll SHA256:");
            var runtimePackDirectory = RuntimeCacheValue(dataDir, "Runtime pack directory:");
            var runtimePackGameAssembly = RuntimeCacheValue(dataDir, "Runtime pack game assembly:");
            var slot = GameRuntimeSlot.Inspect(dataDir, branch);

            var payload = new
            {
                status,
                utc = DateTime.UtcNow.ToString("O"),
                selectedBranch = SteamGameBranch.Normalize(branch),
                selectedVersion = SteamGameBranch.DisplayName(branch),
                selectedVersionSlotKind = SteamGameInstallPaths.VersionSlotKind(branch),
                selectedVersionSlotDirectory = SteamGameInstallPaths.VersionSlotDirectory(dataDir, branch),
                runtimeSlotId = slot.RuntimeSlotId,
                runtimeCacheId = runtimeId,
                selectedPckSha256,
                selectedSourceAssemblySha256,
                activeAndroidAssemblySha256,
                runtimePackId = slot.RuntimePack?.PackId ?? runtimePackDirectory,
                runtimePackStatus = RuntimePackStatus(runtimePackDirectory, runtimePackGameAssembly),
                patchCompatibleBeforeLaunch = !result.CriticalFailed,
                runtimeCompatibleBeforeLaunch = !string.IsNullOrWhiteSpace(activeAndroidAssemblySha256)
                    && !activeAndroidAssemblySha256.StartsWith("<", StringComparison.Ordinal),
                playableBeforeLaunch = !result.CriticalFailed
                    && !string.IsNullOrWhiteSpace(activeAndroidAssemblySha256)
                    && !activeAndroidAssemblySha256.StartsWith("<", StringComparison.Ordinal),
                criticalFailed = result.CriticalFailed,
                hasFailures = result.HasFailures,
                appliedPatchCount = result.AppliedPatchCount,
                failedPatchCount = result.FailedPatchCount,
                totalPatchCount = result.TotalPatchCount,
                durationMs = result.Duration.TotalMilliseconds,
                failureMessages = failures
            };

            File.WriteAllText(
                MarkerPath(dataDir),
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write runtime patch validation evidence: {ex.Message}");
        }
    }
}
