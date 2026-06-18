using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class LauncherBranchCatalog
{
    internal readonly struct BranchOption
    {
        internal BranchOption(
            string branch,
            bool metadataVisible = false,
            int windowsManifestDepotCount = 0,
            string passwordRequired = "",
            string buildId = "",
            string description = "",
            string source = "fallback"
        )
        {
            Branch = SteamGameBranch.Normalize(branch);
            MetadataVisible = metadataVisible;
            WindowsManifestDepotCount = windowsManifestDepotCount;
            PasswordRequired = passwordRequired ?? "";
            BuildId = buildId ?? "";
            Description = description ?? "";
            Source = source ?? "fallback";
        }

        internal string Branch { get; }
        internal bool MetadataVisible { get; }
        internal int WindowsManifestDepotCount { get; }
        internal string PasswordRequired { get; }
        internal string BuildId { get; }
        internal string Description { get; }
        internal string Source { get; }

        internal string Label
            => DropdownLabelWithMetadata();

        private string DropdownLabelWithMetadata()
        {
            var label = SteamGameBranch.DropdownLabel(Branch);
            if (string.Equals(Source, "local install", StringComparison.OrdinalIgnoreCase))
                return $"{label} (installed)";

            if (!string.Equals(Source, "Steam app-info", StringComparison.OrdinalIgnoreCase))
                return label;

            if (string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
                return WindowsManifestDepotCount > 0 ? $"{label} (ready)" : label;

            if (PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
                return $"{label} (password)";

            if (WindowsManifestDepotCount <= 0)
                return $"{label} (unavailable)";

            return !string.IsNullOrWhiteSpace(BuildId)
                ? $"{label} (build {BuildId})"
                : $"{label} (ready)";
        }

        internal string StatusText
        {
            get
            {
                var details = new List<string>();

                if (Source == "Steam app-info")
                {
                    if (PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        details.Add("Download blocked: Steam marks this branch as password-protected, and beta password entry is not implemented yet.");
                        if (WindowsManifestDepotCount > 0)
                            details.Add($"Steam exposed {WindowsManifestDepotCount} Windows depot manifest(s), but the password gate still blocks this launcher from downloading it.");
                    }
                    else if (WindowsManifestDepotCount <= 0)
                    {
                        details.Add(MetadataVisible
                            ? "Download blocked: this branch is visible to this account, but no Windows depot manifest was exposed."
                            : "Download blocked: this branch was not listed in Steam branch metadata for this account.");
                    }
                    else
                    {
                        details.Add($"Downloadable for this account ({WindowsManifestDepotCount} Windows depot manifest(s)).");
                    }

                    if (PasswordRequired.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        details.Add("Steam did not report a password requirement.");
                    }
                    else if (!PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
                        details.Add("Steam did not expose password status.");

                    if (!string.IsNullOrWhiteSpace(BuildId))
                        details.Add($"Build ID: {BuildId}.");

                    if (!string.IsNullOrWhiteSpace(Description))
                        details.Add($"Description: {Description}.");
                }
                else if (!string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(Source, "local install", StringComparison.OrdinalIgnoreCase))
                    {
                        details.Add("Downloaded local install slot is available on this device.");
                        details.Add("Use Refresh Game Versions before redownloading or updating this branch.");
                    }
                    else
                    {
                        details.Add("Steam app-info metadata has not been captured for this option yet. Use Refresh Game Versions before downloading.");
                    }
                }
                else
                {
                    details.Add("Default/public branch remains available even before Steam branch metadata is refreshed.");
                }

                return string.Join(" ", details);
            }
        }
    }

    internal static IReadOnlyList<BranchOption> ReadVisibleBranches(string dataDir)
    {
        var markerPath = SteamGameInstallPaths.BranchAvailabilityMarkerPath(dataDir);
        if (!File.Exists(markerPath))
            return Array.Empty<BranchOption>();

        try
        {
            return File.ReadLines(markerPath)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("Visible branch:", StringComparison.OrdinalIgnoreCase))
                .Select(line => line["Visible branch:".Length..].Trim())
                .Select(BranchOptionFromMarkerValue)
                .Where(option => !string.IsNullOrWhiteSpace(option.Branch))
                .GroupBy(option => option.Branch, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(option => string.Equals(option.Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(option => option.Branch, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<BranchOption>();
        }
    }

    internal static IReadOnlyList<string> ReadVisibleBranchNames(string dataDir)
        => ReadVisibleBranches(dataDir)
            .Select(option => option.Branch)
            .ToArray();

    internal static IReadOnlyList<BranchOption> ReadSelectableBranches(string dataDir)
    {
        var options = new List<BranchOption>();

        foreach (var branch in ReadVisibleBranches(dataDir))
            AddOrReplace(options, branch);

        foreach (var branch in ReadInstalledBranches(dataDir))
            AddIfMissing(options, branch);

        return options
            .OrderBy(option => string.Equals(option.Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(option => option.Branch, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<BranchOption> ReadInstalledBranches(string dataDir)
    {
        var versionsDir = Path.Combine(dataDir, LauncherStorageNames.GameVersionsDirectory);
        if (!Directory.Exists(versionsDir))
            return Array.Empty<BranchOption>();

        var options = new List<BranchOption>();
        try
        {
            foreach (var slotDir in Directory.GetDirectories(versionsDir))
            {
                var markerPath = Path.Combine(
                    slotDir,
                    SteamGameInstallPaths.LegacyPublicGameDirectory,
                    SteamGameInstallPaths.BranchMarkerFileName
                );
                var branch = ReadMarkerValue(markerPath, "Branch:");
                if (string.IsNullOrWhiteSpace(branch))
                    continue;

                branch = SteamGameBranch.Normalize(branch);
                if (string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
                    continue;

                var expectedDirectoryName = SteamGameBranch.StateDirectoryName(branch);
                var actualDirectoryName = Path.GetFileName(slotDir);
                if (!string.Equals(actualDirectoryName, expectedDirectoryName, StringComparison.OrdinalIgnoreCase))
                    continue;

                AddIfMissing(options, new BranchOption(branch, source: "local install"));
            }
        }
        catch
        {
            return Array.Empty<BranchOption>();
        }

        return options;
    }

    internal static IReadOnlyList<BranchOption> DropdownOptions(
        string selectedBranch,
        IReadOnlyList<BranchOption> discoveredBranches
    )
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        var options = new List<BranchOption>
        {
            new(SteamGameBranch.Public, source: "fallback")
        };

        foreach (var branch in discoveredBranches ?? Array.Empty<BranchOption>())
            AddOrReplace(options, branch);

        AddIfMissing(options, new BranchOption(selectedBranch, source: "saved selection"));

        return options;
    }

    internal static string SourceDescription(string dataDir)
        => ReadVisibleBranches(dataDir).Count == 0
            ? "default options; no Steam app-info branch catalog captured yet"
            : "Steam app-info visible branch catalog";

    private static BranchOption BranchOptionFromMarkerValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new BranchOption("");

        var nameEnd = value.IndexOf(" [", StringComparison.Ordinal);
        var name = (nameEnd > 0 ? value[..nameEnd] : value).Trim();
        var metadata = nameEnd > 0 && value.EndsWith("]", StringComparison.Ordinal)
            ? ParseMetadata(value[(nameEnd + 2)..^1])
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new BranchOption(
            name,
            metadataVisible: BoolValue(metadata, "metadataVisible"),
            windowsManifestDepotCount: IntValue(metadata, "windowsManifestDepots"),
            passwordRequired: Value(metadata, "passwordRequired"),
            buildId: Value(metadata, "buildId"),
            description: Value(metadata, "description"),
            source: "Steam app-info"
        );
    }

    private static Dictionary<string, string> ParseMetadata(string metadata)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in metadata.Split(','))
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
                continue;

            var key = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
                values[key] = value;
        }

        return values;
    }

    private static void AddIfMissing(List<BranchOption> options, BranchOption option)
    {
        if (!options.Exists(existing => string.Equals(existing.Branch, option.Branch, StringComparison.OrdinalIgnoreCase)))
            options.Add(option);
    }

    private static void AddOrReplace(List<BranchOption> options, BranchOption option)
    {
        var existingIndex = options.FindIndex(existing => string.Equals(existing.Branch, option.Branch, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            options[existingIndex] = option;
        else
            options.Add(option);
    }

    private static bool BoolValue(Dictionary<string, string> values, string key)
        => Value(values, key).Equals("true", StringComparison.OrdinalIgnoreCase);

    private static int IntValue(Dictionary<string, string> values, string key)
        => int.TryParse(Value(values, key), out var value) ? value : 0;

    private static string Value(Dictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : "";

    private static string ReadMarkerValue(string path, string prefix)
    {
        try
        {
            if (!File.Exists(path))
                return "";

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line[prefix.Length..].Trim();
            }
        }
        catch
        {
            return "";
        }

        return "";
    }

    internal static string DropdownOptionLabels(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
        => string.Join(" | ", DropdownOptions(selectedBranch, discoveredBranches).Select(option => option.Label));

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

    internal static string DropdownOptionMetadata(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
    {
        var options = DropdownOptions(selectedBranch, discoveredBranches);
        return string.Join(
            " | ",
            options.Select(option =>
                $"{option.Branch}:source={option.Source};metadataVisible={option.MetadataVisible.ToString().ToLowerInvariant()};windowsManifestDepots={option.WindowsManifestDepotCount};passwordRequired={ValueOrUnknown(option.PasswordRequired)};buildId={ValueOrNone(option.BuildId)}"
            )
        );
    }

    private static string ValueOrUnknown(string value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();

    private static string ValueOrNone(string value)
        => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
}
