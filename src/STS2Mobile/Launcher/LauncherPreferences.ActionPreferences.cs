using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    internal readonly struct ActionPreferences
    {
        internal ActionPreferences(bool localBackupEnabled, bool cloudSyncEnabled, string gameBranch)
        {
            LocalBackupEnabled = localBackupEnabled;
            CloudSyncEnabled = cloudSyncEnabled;
            GameBranch = SteamGameBranch.Normalize(gameBranch);
        }

        internal bool LocalBackupEnabled { get; }
        internal bool CloudSyncEnabled { get; }
        internal string GameBranch { get; }
    }

    internal static ActionPreferences ReadActionPreferences()
        => new(
            LocalBackupPreference.Read(),
            CloudSyncPreference.Read(),
            ReadGameBranch()
        );

    internal static ActionPreferences LoadAndApplyActionPreferences()
        => new(
            LocalBackupPreference.LoadAndApply(),
            CloudSyncPreference.LoadAndApply(),
            ReadGameBranch()
        );
}
