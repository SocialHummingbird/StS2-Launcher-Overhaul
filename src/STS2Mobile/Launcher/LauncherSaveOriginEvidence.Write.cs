using System;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherSaveOriginEvidence
{
    internal static void WriteManualPullOrigin(string dataDir, string selectedBranch)
        => WriteOrigin(dataDir, selectedBranch, "manual cloud pull");

    internal static void WriteManualPushOrigin(string dataDir, string selectedBranch)
        => WriteOrigin(dataDir, selectedBranch, "manual cloud push completed from Android local saves");

    internal static void WriteBranchSwitchPendingOrigin(string dataDir, string previousBranch, string selectedBranch)
    {
        try
        {
            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + "Origin action: branch switch pending Pull\n"
                + $"Previous branch: {SteamGameBranch.Normalize(previousBranch)}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + "Current Android local saves verified for selected branch: false\n"
                + "Required next action: Pull from Cloud for the selected game version before Push.\n";
            File.WriteAllText(MarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write save-origin branch-switch marker: {ex.Message}");
        }
    }

    private static void WriteOrigin(string dataDir, string selectedBranch, string originAction)
    {
        try
        {
            selectedBranch = SteamGameBranch.Normalize(selectedBranch);
            var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
            var importantLocalSaveEvidencePresent = LauncherLocalSaveEvidence.HasImportantSaveEvidence(dataDir);
            var selectedBranchVerified = importantLocalSaveEvidencePresent;
            var selectedRuntimePlayable = slot.Playable;
            var selectedRuntimeVerified = selectedBranchVerified && selectedRuntimePlayable;
            var text =
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Origin action: {originAction}\n"
                + $"Selected branch: {selectedBranch}\n"
                + $"Selected branch selection kind: {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"Steam branch selector mode: {SteamGameBranch.SelectorMode}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected runtime slot ID: {slot.RuntimeSlotId}\n"
                + $"Selected PCK SHA256: {slot.PckSha256}\n"
                + $"Selected source sts2.dll SHA256: {slot.SourceAssemblySha256}\n"
                + $"Selected runtime playable: {selectedRuntimePlayable.ToString().ToLowerInvariant()}\n"
                + $"Selected runtime readiness problem: {slot.ReadinessProblem() ?? string.Empty}\n"
                + $"Important Android local save evidence count: {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}\n"
                + $"Current Android local saves verified for selected branch: {selectedBranchVerified.ToString().ToLowerInvariant()}\n"
                + $"Current Android local saves verified for selected runtime: {selectedRuntimeVerified.ToString().ToLowerInvariant()}\n";
            File.WriteAllText(MarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write save-origin marker: {ex.Message}");
        }
    }
}
