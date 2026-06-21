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
                $"{UtcPrefix} {DateTime.UtcNow:O}\n"
                + $"{OriginActionPrefix} branch switch pending Pull\n"
                + $"{PreviousBranchPrefix} {SteamGameBranch.Normalize(previousBranch)}\n"
                + $"{SelectedBranchPrefix} {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"{SelectedVersionPrefix} {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"{SelectedVersionSlotKindPrefix} {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"{SelectedVersionSlotDirectoryPrefix} {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"{CurrentLocalSavesVerifiedForSelectedBranchPrefix} false\n"
                + $"{RequiredNextActionPrefix} Pull from Cloud for the selected game version before Push.\n";
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
                $"{UtcPrefix} {DateTime.UtcNow:O}\n"
                + $"{OriginActionPrefix} {originAction}\n"
                + $"{SelectedBranchPrefix} {selectedBranch}\n"
                + $"{SelectedBranchSelectionKindPrefix} {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"{SelectorModePrefix} {SteamGameBranch.SelectorMode}\n"
                + $"{SelectedVersionPrefix} {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"{SelectedVersionSlotKindPrefix} {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"{SelectedVersionSlotDirectoryPrefix} {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"{SelectedRuntimeSlotIdPrefix} {slot.RuntimeSlotId}\n"
                + $"{SelectedPckSha256Prefix} {slot.PckSha256}\n"
                + $"{SelectedSourceAssemblySha256Prefix} {slot.SourceAssemblySha256}\n"
                + $"{SelectedRuntimePlayablePrefix} {selectedRuntimePlayable.ToString().ToLowerInvariant()}\n"
                + $"{SelectedRuntimeReadinessProblemPrefix} {slot.ReadinessProblem() ?? string.Empty}\n"
                + $"{ImportantLocalSaveEvidenceCountPrefix} {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}\n"
                + $"{CurrentLocalSavesVerifiedForSelectedBranchPrefix} {selectedBranchVerified.ToString().ToLowerInvariant()}\n"
                + $"{CurrentLocalSavesVerifiedForSelectedRuntimePrefix} {selectedRuntimeVerified.ToString().ToLowerInvariant()}\n";
            File.WriteAllText(MarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write save-origin marker: {ex.Message}");
        }
    }
}
