using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherCloudSyncCoordinator
{
    private readonly partial struct ManualCloudSyncRequest
    {
        private static string PushConfirmationMessage(string dataDir, string selectedBranch)
            => "Push Android local saves to Steam Cloud?\n"
                + $"Selected game version: {SteamGameBranch.DisplayName(selectedBranch)}.\n"
                + "This can overwrite Steam Cloud saves for this Steam account. "
                + "Confirm that the selected game version and play mode match the local saves you intend to upload.";
    }
}
