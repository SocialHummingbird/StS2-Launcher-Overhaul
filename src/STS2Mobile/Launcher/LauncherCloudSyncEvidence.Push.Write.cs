using System;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
    internal static void WriteManualPushMarker(string dataDir, string selectedBranch)
    {
        try
        {
            LauncherSaveOriginEvidence.WriteManualPushOrigin(dataDir, selectedBranch);
            var text =
                $"{UtcPrefix} {DateTime.UtcNow:O}\n"
                + $"{SelectedBranchPrefix} {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"{SelectedBranchSelectionKindPrefix} {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"{SelectorModePrefix} {SteamGameBranch.SelectorMode}\n"
                + $"{SelectedVersionPrefix} {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"{SelectedVersionSlotKindPrefix} {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"{SelectedVersionSlotDirectoryPrefix} {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"{SelectedBranchNotePrefix} {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + $"{PrePushLocalBackupEvidenceCountPrefix} {LauncherBackupEvidence.LocalPrePushBackupCount()}\n"
                + $"{PrePushCloudBackupEvidenceCountPrefix} {LauncherBackupEvidence.CloudPrePushBackupCount()}\n"
                + $"{LatestPrePushLocalBackupUtcPrefix} {LauncherBackupEvidence.LatestLocalPrePushBackupUtc()}\n"
                + $"{LatestPrePushCloudBackupUtcPrefix} {LauncherBackupEvidence.LatestCloudPrePushBackupUtc()}\n"
                + $"{ImportantLocalSaveEvidenceCountPrefix} {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}\n"
                + $"{BaselineManualPushPrerequisitesSatisfiedPrefix} {BaselineManualPushPrerequisitesSatisfied(dataDir, selectedBranch).ToString().ToLowerInvariant()}\n"
                + $"{BranchSwitchPrePushBackupEvidenceSatisfiedPrefix} {LauncherBackupEvidence.HasPrePushBackupEvidenceAfterBranchSwitch(dataDir).ToString().ToLowerInvariant()}\n"
                + $"{ManualPushCompletedAfterBranchSwitchSafetyGatesPrefix} true\n";
            File.WriteAllText(LastManualPushMarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual Push evidence marker: {ex.Message}");
        }
    }
}
