using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly partial struct ManualCloudSyncRequest
    {
        private static string PushConfirmationMessage(string dataDir, string selectedBranch)
            => "Push Android local saves to Steam Cloud?\n"
                + $"Selected game version: {SteamGameBranch.DisplayName(selectedBranch)}.\n"
                + $"Selected version slot: {SteamGameInstallPaths.VersionSlotKind(selectedBranch)} ({SteamGameBranch.StateDirectoryName(selectedBranch)}).\n"
                + BranchSwitchPushWarning(dataDir, selectedBranch)
                + "This can overwrite Steam Cloud saves for this Steam account. "
                + "Save compatibility across Steam branches is not validated. "
                + "When Local Backup is ON, manual Push backs up important Android local saves and existing Steam Cloud saves before upload. "
                + "Pull from Cloud first and verify the Android saves exist before pushing.";

        private static string BranchSwitchPushWarning(string dataDir, string selectedBranch)
            => LauncherBranchSwitchSafety.HasMarker(dataDir)
                ? "A game version switch was recorded on this install. Treat this Push as cross-version/destructive unless the selected-version safety evidence is current: "
                    + $"Pull-after-switch for {SteamGameBranch.DisplayName(selectedBranch)}, Android local save evidence, backup storage permission, local pre-Push backup evidence, and cloud pre-Push backup evidence.\n"
                : string.Empty;
    }
}
