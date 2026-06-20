using System;
using System.Collections.Generic;
using System.Text;

namespace STS2Mobile.Steam;

internal static class SteamGameBranch
{
    internal const string Public = "public";
    internal const string Beta = "beta";
    internal const bool BetaPasswordEntrySupported = false;
    internal const bool BranchDiscoverySupported = true;
    internal const string SelectorMode = "Steam branch dropdown";

    internal static string Normalize(string branch)
    {
        if (string.IsNullOrWhiteSpace(branch))
            return Public;

        return branch.Trim();
    }

    internal static string ToggleKnownBranch(string branch)
        => string.Equals(Normalize(branch), Public, StringComparison.OrdinalIgnoreCase)
            ? Beta
            : Public;

    internal static string DisplayName(string branch)
    {
        branch = Normalize(branch);
        return string.Equals(branch, Public, StringComparison.OrdinalIgnoreCase)
            ? "Default"
            : branch;
    }

    internal static string CompactDisplayName(string branch, int maxLength = 24)
    {
        var displayName = DisplayName(branch);
        if (displayName.Length <= maxLength)
            return displayName;

        return displayName[..Math.Max(1, maxLength - 3)] + "...";
    }

    internal static string SelectionKind(string branch)
        => string.Equals(Normalize(branch), Public, StringComparison.OrdinalIgnoreCase)
            ? "dropdown default/public branch"
            : "dropdown non-public branch";

    internal static IReadOnlyList<string> DropdownBranches(string selectedBranch)
        => DropdownBranches(selectedBranch, Array.Empty<string>());

    internal static IReadOnlyList<string> DropdownBranches(string selectedBranch, IEnumerable<string> discoveredBranches)
    {
        selectedBranch = Normalize(selectedBranch);
        var branches = new List<string> { Public };

        foreach (var discoveredBranch in discoveredBranches ?? Array.Empty<string>())
        {
            var branch = Normalize(discoveredBranch);
            if (!branches.Exists(candidate => string.Equals(candidate, branch, StringComparison.OrdinalIgnoreCase)))
                branches.Add(branch);
        }

        if (!branches.Exists(branch => string.Equals(branch, selectedBranch, StringComparison.OrdinalIgnoreCase)))
            branches.Add(selectedBranch);

        return branches;
    }

    internal static string DropdownLabel(string branch)
    {
        branch = Normalize(branch);
        return string.Equals(branch, Public, StringComparison.OrdinalIgnoreCase)
            ? "Default / public"
            : $"{DisplayName(branch)}";
    }

    internal static string SelectorHelpText(string branch)
    {
        branch = Normalize(branch);
        return string.Equals(branch, Public, StringComparison.OrdinalIgnoreCase)
            ? "Default/public Steam branch. Choose a game version from the dropdown. Account-visible branch options refresh after Steam app-info is available; beta password entry is still being hardened."
            : $"Steam branch '{branch}' selected from the game version dropdown. Private/password-protected branches may be inaccessible because beta password entry is not supported. Failed downloads do not change Steam Cloud saves. Save compatibility is unproven.";
    }

    internal static string SelectorInstallSlotHelpText(string branch)
    {
        branch = Normalize(branch);
        return SelectorHelpText(branch)
            + $"\nActive install slot: {SteamGameInstallPaths.VersionSlotKind(branch)} ({StateDirectoryName(branch)}).";
    }

    internal static string StateDirectoryName(string branch)
    {
        var storageBranch = StorageIdentity(branch);
        if (string.Equals(storageBranch, Public, StringComparison.OrdinalIgnoreCase))
            return Public;

        if (string.Equals(storageBranch, Beta, StringComparison.OrdinalIgnoreCase))
            return Beta;

        var sb = new StringBuilder(storageBranch.Length);

        foreach (var ch in storageBranch)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.')
                sb.Append(ch);
            else
                sb.Append('_');
        }

        var safePrefix = sb.Length == 0 ? "branch" : sb.ToString();
        if (safePrefix.Length > 48)
            safePrefix = safePrefix[..48].TrimEnd('.', '-', '_');

        if (safePrefix.Length == 0)
            safePrefix = "branch";

        return $"{safePrefix}-{StableBranchHash(storageBranch)}";
    }

    internal static string StorageIdentity(string branch)
        => Normalize(branch).ToLowerInvariant();

    private static string StableBranchHash(string branch)
    {
        unchecked
        {
            const uint offsetBasis = 2166136261;
            const uint prime = 16777619;

            var hash = offsetBasis;
            foreach (var ch in branch)
            {
                hash ^= ch;
                hash *= prime;
            }

            return hash.ToString("x8");
        }
    }
}
