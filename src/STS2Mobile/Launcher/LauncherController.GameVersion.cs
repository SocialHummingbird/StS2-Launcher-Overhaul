using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private static string SelectedGameVersionName()
        => SteamGameBranch.DisplayName(LauncherPreferences.ReadGameBranch());
}
