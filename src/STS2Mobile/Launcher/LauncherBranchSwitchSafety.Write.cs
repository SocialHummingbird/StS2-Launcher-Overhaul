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
                $"UTC: {DateTime.UtcNow:O}\n"
                + $"Previous branch: {SteamGameBranch.Normalize(previousBranch)}\n"
                + $"Selected branch: {SteamGameBranch.Normalize(selectedBranch)}\n"
                + $"Selected branch selection kind: {SteamGameBranch.SelectionKind(selectedBranch)}\n"
                + $"Steam branch selector mode: {SteamGameBranch.SelectorMode}\n"
                + $"Selected version: {SteamGameBranch.DisplayName(selectedBranch)}\n"
                + $"Selected version slot kind: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)}\n"
                + $"Selected version slot directory: {SteamGameInstallPaths.VersionSlotDirectory(dataDir, selectedBranch)}\n"
                + $"Selected branch note: {SteamGameBranch.SelectorHelpText(selectedBranch)}\n"
                + "Local backup forced on: true\n"
                + "Manual Push requires backup storage: true\n"
                + "Warning acknowledged: branch switch can require another download and saves may not be compatible between Steam branches.\n"
                + "Non-public branch warning acknowledged: branch may be private or password-protected; beta password entry is not implemented.\n";
            File.WriteAllText(MarkerPath(dataDir), text);
            LauncherSaveOriginEvidence.WriteBranchSwitchPendingOrigin(dataDir, previousBranch, selectedBranch);
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write branch switch safety marker: {ex.Message}");
        }
    }
}
