using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherPreferences
{
    internal static string ReadGameBranch()
        => SteamGameBranch.Normalize(GameBranchPreference.ReadText(SteamGameBranch.Public));

    internal static void SaveGameBranch(string branch)
        => GameBranchPreference.WriteText(SteamGameBranch.Normalize(branch));

    internal static bool GameBranchPreferenceExists()
        => GameBranchPreference.Exists();
}
