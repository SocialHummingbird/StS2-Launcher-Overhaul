using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static string CompactRetryButtonText()
        => CompactPlaySyncDrawerText("Try Again", "Restart task");

    private static string CompactLaunchButtonText(string text)
        => CompactPlaySyncDrawerText(CompactLaunchTitle(text), "Ready version");

    private static string CompactLaunchTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Start Game";

        var normalized = text.Trim();
        return string.Equals(normalized, "Start Game", StringComparison.OrdinalIgnoreCase)
            ? "Start Game"
            : normalized;
    }

    private static string CompactPlaySyncDrawerText(string action, string detail)
        => $"{action}\n{detail}";

    private string CompactReadyVersionSummary()
    {
        if (_compactStackedActionRows)
        {
            return $"Ready: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactReadyStackedSummaryBranchLimit)}\n"
                + "Save Check | Upload locked | no auto cloud upload";
        }

        return $"Ready: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactReadySummaryBranchLimit)} | Save Check | Upload locked";
    }

    private string CompactReadyVersionHelpText()
    {
        var branchLimit = _compactStackedActionRows
            ? CompactReadyVersionHelpStackedBranchLimit
            : CompactReadyVersionHelpBranchLimit;

        return $"Play version: {SteamGameBranch.CompactDisplayName(_gameBranch, branchLimit)} | {CompactReadyFileScope(_gameBranch)}\n"
            + $"{LauncherBranchCatalog.SelectedOptionCompactStatus(_gameBranch, _availableBranches)} | Saves: Get/Upload";
    }

    private static string CompactReadyFileScope(string branch)
        => string.Equals(SteamGameBranch.Normalize(branch), SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? "Default files"
            : "Separate files";
}
