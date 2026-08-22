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
