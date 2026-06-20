using System;
using System.Collections.Generic;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    internal static string SelectedOptionStatus(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        var hasRefreshedCatalog = (discoveredBranches ?? Array.Empty<BranchOption>()).Count > 0;
        var option = DropdownOptions(selectedBranch, discoveredBranches)
            .FirstOrDefault(candidate => string.Equals(candidate.Branch, selectedBranch, StringComparison.OrdinalIgnoreCase));

        if (
            hasRefreshedCatalog
            && string.Equals(option.Source, "saved selection", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(selectedBranch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
        )
            return "Selected saved branch was not listed in the latest Steam app-info catalog for this account. It may be stale, private, inaccessible, password-protected, or unavailable; Refresh Game Versions again or choose an account-visible branch before downloading.";

        return string.IsNullOrWhiteSpace(option.Branch)
            ? "Steam app-info metadata is unavailable for the selected game version."
            : option.StatusText;
    }

    internal static string SelectedOptionCompactStatus(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        var branches = discoveredBranches ?? Array.Empty<BranchOption>();
        var hasRefreshedCatalog = branches.Count > 0;
        var option = DropdownOptions(selectedBranch, discoveredBranches)
            .FirstOrDefault(candidate => string.Equals(candidate.Branch, selectedBranch, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(option.Branch))
            return "Metadata unavailable";

        if (
            hasRefreshedCatalog
            && string.Equals(option.Source, "saved selection", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(selectedBranch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
        )
            return "Not in refreshed catalog";

        if (string.Equals(option.Source, "Steam app-info", StringComparison.OrdinalIgnoreCase))
        {
            if (option.PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
                return "Password branch blocked";

            if (option.WindowsManifestDepotCount <= 0)
                return option.MetadataVisible ? "No Windows depot" : "Not visible to account";

            return string.IsNullOrWhiteSpace(option.BuildId)
                ? "Ready in Steam catalog"
                : $"Ready build {SteamGameBranch.CompactDisplayName(option.BuildId, 12)}";
        }

        if (string.Equals(option.Source, "local install", StringComparison.OrdinalIgnoreCase))
            return "Installed on device";

        if (string.Equals(selectedBranch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
            return "Default branch ready";

        return "Refresh before download";
    }

    internal static string SelectedOptionDownloadProblem(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        if (string.Equals(selectedBranch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
            return "";

        var branches = discoveredBranches ?? Array.Empty<BranchOption>();
        var hasRefreshedCatalog = branches.Count > 0;
        var option = branches.FirstOrDefault(candidate => string.Equals(candidate.Branch, selectedBranch, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(option.Branch))
            return hasRefreshedCatalog
                ? "Download blocked: selected saved branch was not listed in the latest Steam app-info catalog for this account. Refresh Game Versions again or choose an account-visible branch."
                : "";

        if (option.PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
            return "Download blocked: selected branch is password-protected, and Steam beta password entry is not implemented yet.";

        if (option.WindowsManifestDepotCount <= 0)
            return "Download blocked: selected branch has no Windows depot manifest visible to this Steam account.";

        return "";
    }
}
