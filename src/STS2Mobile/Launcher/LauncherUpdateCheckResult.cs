using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal readonly struct LauncherUpdateCheckResult
{
    internal LauncherUpdateCheckResult(string branch, bool hasUpdate)
    {
        Branch = SteamGameBranch.Normalize(branch);
        HasUpdate = hasUpdate;
    }

    internal string Branch { get; }
    internal bool HasUpdate { get; }
}
