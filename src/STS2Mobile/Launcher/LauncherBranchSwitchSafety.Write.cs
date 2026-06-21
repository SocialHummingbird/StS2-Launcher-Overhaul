using System;
using System.IO;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchSwitchSafety
{
    internal static void WriteMarker(string dataDir, string previousBranch, string selectedBranch)
    {
        try
        {
            var text =
                $"{UtcPrefix} {DateTime.UtcNow:O}\n"
                + $"{PreviousBranchPrefix} {SteamGameBranch.Normalize(previousBranch)}\n"
                + $"{SelectedBranchPrefix} {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"{SelectedBranchSelectionKindPrefix} {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"{SelectorModePrefix} {SteamGameBranch.SelectorMode}\n"
                + $"{SelectedVersionPrefix} {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"{SelectedVersionSlotKindPrefix} {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"{SelectedVersionSlotDirectoryPrefix} {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"{SelectedBranchNotePrefix} {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + $"{LocalBackupForcedPrefix} true\n"
                + $"{ManualPushRequiresBackupStoragePrefix} true\n"
                + $"{WarningAcknowledgedPrefix} branch switch can require another download and saves may not be compatible between Steam branches.\n"
                + $"{NonPublicBranchWarningAcknowledgedPrefix} branch may be private or password-protected; beta password entry is not implemented.\n";
            File.WriteAllText(MarkerPath(dataDir), text);
            LauncherSaveOriginEvidence.WriteBranchSwitchPendingOrigin(dataDir, previousBranch, selectedBranch);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write branch switch safety marker: {ex.Message}");
        }
    }
}
