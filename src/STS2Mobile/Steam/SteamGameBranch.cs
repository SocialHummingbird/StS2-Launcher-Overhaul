using System;
using System.Text;

namespace STS2Mobile.Steam;

internal static class SteamGameBranch
{
    internal const string Public = "public";
    internal const string Beta = "beta";
    internal const bool BetaPasswordEntrySupported = false;
    internal const bool BranchDiscoverySupported = false;
    internal const string SelectorMode = "public/beta toggle";

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

    internal static string SelectorHelpText(string branch)
    {
        branch = Normalize(branch);
        return string.Equals(branch, Public, StringComparison.OrdinalIgnoreCase)
            ? "Default/public Steam branch. Beta toggle is fixed; branch discovery is not supported."
            : "Beta branch selected. Private/password-protected beta branches are not supported; inaccessible beta branches fail during download/update checks without changing Steam Cloud saves. Save compatibility is unproven.";
    }

    internal static string SelectorInstallSlotHelpText(string branch)
    {
        branch = Normalize(branch);
        return SelectorHelpText(branch)
            + $"\nActive install slot: {SteamGameInstallPaths.VersionSlotKind(branch)} ({StateDirectoryName(branch)}).";
    }

    internal static string StateDirectoryName(string branch)
    {
        branch = Normalize(branch);
        if (string.Equals(branch, Public, StringComparison.OrdinalIgnoreCase))
            return Public;

        if (string.Equals(branch, Beta, StringComparison.OrdinalIgnoreCase))
            return Beta;

        var sb = new StringBuilder(branch.Length);

        foreach (var ch in branch)
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

        return $"{safePrefix}-{StableBranchHash(branch)}";
    }

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
