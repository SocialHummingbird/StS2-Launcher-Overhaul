using System;
using System.IO;

namespace STS2Mobile.Steam;

internal static class SteamGameInstallPaths
{
    internal const string LegacyPublicGameDirectory = "game";
    internal const string GameVersionsDirectory = "game_versions";
    internal const string DownloadStateDirectory = "download_state";
    internal const string BranchMarkerFileName = "steam_branch.txt";

    internal static string VersionSlotDirectory(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        return string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? dataDir
            : Path.Combine(dataDir, GameVersionsDirectory, SteamGameBranch.StateDirectoryName(branch));
    }

    internal static string VersionSlotKind(string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        return string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? "public legacy"
            : "side-by-side branch cache";
    }

    internal static string GameDirectory(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        return string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(dataDir, LegacyPublicGameDirectory)
            : Path.Combine(dataDir, GameVersionsDirectory, SteamGameBranch.StateDirectoryName(branch), LegacyPublicGameDirectory);
    }

    internal static string DownloadStateDirectoryPath(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        return string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(dataDir, DownloadStateDirectory)
            : Path.Combine(dataDir, GameVersionsDirectory, SteamGameBranch.StateDirectoryName(branch), DownloadStateDirectory);
    }

    internal static string BranchMarkerPath(string dataDir, string branch)
        => Path.Combine(GameDirectory(dataDir, branch), BranchMarkerFileName);
}
