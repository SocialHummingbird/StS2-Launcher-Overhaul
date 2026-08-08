using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed class LauncherLaunchReadinessCacheKey
{
    private LauncherLaunchReadinessCacheKey(
        string dataDir,
        string branch,
        LauncherLaunchReadinessCacheIdentities identities
    )
    {
        DataDir = dataDir ?? string.Empty;
        Branch = SteamGameBranch.Normalize(branch);
        Identities = identities;
    }

    internal string Branch { get; }

    private string DataDir { get; }
    private LauncherLaunchReadinessCacheIdentities Identities { get; }

    internal static LauncherLaunchReadinessCacheKey Create(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        return new LauncherLaunchReadinessCacheKey(
            dataDir,
            branch,
            LauncherLaunchReadinessCacheIdentities.Capture(dataDir, branch)
        );
    }

    internal bool TargetMatches(string dataDir, string branch)
        => string.Equals(DataDir, dataDir ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(Branch, SteamGameBranch.Normalize(branch), StringComparison.OrdinalIgnoreCase);

    internal bool MatchesCurrent(string dataDir, string branch, out string mismatchReason)
    {
        mismatchReason = string.Empty;
        branch = SteamGameBranch.Normalize(branch);
        if (!TargetMatches(dataDir, branch))
        {
            mismatchReason = "target changed";
            return false;
        }

        return Identities.MatchesCurrent(dataDir, branch, out mismatchReason);
    }
}
