using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherGameVersionDisplay
{
    internal static string SelectedGameVersionName()
        => SteamGameBranch.DisplayName(LauncherPreferences.ReadGameBranch());
}
