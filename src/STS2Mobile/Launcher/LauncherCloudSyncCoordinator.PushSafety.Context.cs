using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly struct CloudPushSafetyContext
    {
        private CloudPushSafetyContext(string dataDir, string selectedBranch)
        {
            DataDir = dataDir;
            SelectedBranch = selectedBranch;
            SelectedVersion = SteamGameBranch.DisplayName(selectedBranch);
        }

        internal string DataDir { get; }
        internal string SelectedBranch { get; }
        internal string SelectedVersion { get; }

        internal static CloudPushSafetyContext Create(string dataDir)
            => new(dataDir, LauncherPreferences.ReadGameBranch());

        internal void WriteBlockedMarker(string reason)
            => LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(
                DataDir,
                SelectedBranch,
                reason
            );
    }
}
