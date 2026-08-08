using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class DownloadSection
{
    private string CompactSelectedVersionHeadline()
    {
        if (_compactStackedActionRows)
        {
            return $"Version: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactSelectedVersionStackedBranchLimit)}\n"
                + $"{CompactInstallFileScope(_gameBranch)} | Cloud unchanged | Change version";
        }

        return $"Version: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactSelectedVersionBranchLimit)} | {CompactInstallFileScope(_gameBranch)} | Cloud unchanged | Change";
    }

    private string CompactInstallVersionHelpText()
    {
        var branchLimit = _compactStackedActionRows
            ? CompactVersionHelpStackedBranchLimit
            : CompactVersionHelpBranchLimit;

        return $"Files for: {SteamGameBranch.CompactDisplayName(_gameBranch, branchLimit)} | {CompactInstallFileScope(_gameBranch)}\n"
            + $"{LauncherBranchCatalog.SelectedOptionCompactStatus(_gameBranch, _availableBranches)} | Saves unchanged";
    }

    private static string CompactInstallFileScope(string branch)
        => string.Equals(SteamGameBranch.Normalize(branch), SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? "Default files"
            : "Separate files";
}
