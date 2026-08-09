using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal sealed partial class ActionSection
{
    private static string CompactRetryButtonText()
        => CompactPlaySyncDrawerText("Try Again", "Restart task");

    private static string CompactLaunchButtonText(string text)
        => CompactPlaySyncDrawerText(LaunchTitle(text), "Ready version");

    private static string LaunchTitle(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Play";

        var normalized = text.Trim();
        return string.Equals(normalized, "Start Game", StringComparison.OrdinalIgnoreCase)
            ? "Play"
            : normalized;
    }

    private static string CompactPlaySyncDrawerText(string action, string detail)
        => $"{action}\n{detail}";

    private string CompactReadyVersionSummary()
    {
        var activeMods = _readySummaryEnabledModCount;
        var modSummary = activeMods > 0 ? $" | Mods {activeMods}" : " | Mods off";
        if (_compactStackedActionRows)
        {
            return $"Ready: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactReadyStackedSummaryBranchLimit)}\n"
                + $"Play{modSummary}";
        }

        return $"Ready: {SteamGameBranch.CompactDisplayName(_gameBranch, CompactReadySummaryBranchLimit)} | Play{modSummary}";
    }

    private string CompactReadyVersionHelpText()
    {
        var branchLimit = _compactStackedActionRows
            ? CompactReadyVersionHelpStackedBranchLimit
            : CompactReadyVersionHelpBranchLimit;

        return $"Play version: {SteamGameBranch.CompactDisplayName(_gameBranch, branchLimit)} | {CompactReadyFileScope(_gameBranch)}\n"
            + LauncherBranchCatalog.SelectedOptionCompactStatus(_gameBranch, _availableBranches);
    }

    private static string CompactReadyFileScope(string branch)
        => string.Equals(SteamGameBranch.Normalize(branch), SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
            ? "Default files"
            : "Separate files";
}
