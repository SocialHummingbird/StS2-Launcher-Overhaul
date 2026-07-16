using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    internal readonly struct ActionPreferences
    {
        internal ActionPreferences(
            bool localBackupEnabled,
            bool cloudSyncEnabled,
            string gameBranch,
            string rendererMode
        )
        {
            LocalBackupEnabled = localBackupEnabled;
            CloudSyncEnabled = cloudSyncEnabled;
            GameBranch = SteamGameBranch.Normalize(gameBranch);
            RendererMode = LauncherRendererMode.Normalize(rendererMode);
        }

        internal bool LocalBackupEnabled { get; }
        internal bool CloudSyncEnabled { get; }
        internal string GameBranch { get; }
        internal string RendererMode { get; }
    }

    internal static ActionPreferences ReadActionPreferences()
        => new(
            LocalBackupPreference.Read(),
            CloudSyncPreference.Read(),
            ReadGameBranch(),
            ReadRendererMode()
        );

    internal static ActionPreferences ReadActionPreferences(string gameBranch)
        => new(
            LocalBackupPreference.Read(),
            CloudSyncPreference.Read(),
            gameBranch,
            ReadRendererMode()
        );

    internal static ActionPreferences LoadAndApplyActionPreferences()
        => new(
            LocalBackupPreference.LoadAndApply(),
            CloudSyncPreference.LoadAndApply(),
            ReadGameBranch(),
            ReadRendererMode()
        );
}
