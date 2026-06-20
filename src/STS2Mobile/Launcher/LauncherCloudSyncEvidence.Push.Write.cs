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
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected branch selection kind: {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"Steam branch selector mode: {SteamGameBranch.SelectorMode}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected branch note: {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + $"Pre-Push local backup evidence count: {LauncherBackupEvidence.LocalPrePushBackupCount()}\n"
                + $"Pre-Push cloud backup evidence count: {LauncherBackupEvidence.CloudPrePushBackupCount()}\n"
                + $"Latest pre-Push local backup UTC: {LauncherBackupEvidence.LatestLocalPrePushBackupUtc()}\n"
                + $"Latest pre-Push cloud backup UTC: {LauncherBackupEvidence.LatestCloudPrePushBackupUtc()}\n"
                + $"Important Android local save evidence count: {LauncherLocalSaveEvidence.CountImportantSaveEvidence(dataDir)}\n"
                + $"Baseline manual Push prerequisites satisfied: {BaselineManualPushPrerequisitesSatisfied(dataDir, selectedBranch).ToString().ToLowerInvariant()}\n"
                + $"Branch-switch pre-Push backup evidence satisfied: {LauncherBackupEvidence.HasPrePushBackupEvidenceAfterBranchSwitch(dataDir).ToString().ToLowerInvariant()}\n"
                + "Manual Push completed after branch-switch safety gates: true\n";
            File.WriteAllText(LastManualPushMarkerPath(dataDir), text);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write manual Push evidence marker: {ex.Message}");
        }
    }
}
