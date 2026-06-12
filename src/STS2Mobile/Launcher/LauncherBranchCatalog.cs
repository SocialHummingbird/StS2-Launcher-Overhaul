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
            => SteamGameBranch.DropdownLabel(Branch);

        internal string StatusText
        {
            get
            {
                var details = new List<string>();

                if (Source == "Steam app-info")
                {
                    details.Add(WindowsManifestDepotCount > 0
                        ? $"Downloadable for this account ({WindowsManifestDepotCount} Windows depot manifest(s))."
                        : MetadataVisible
                            ? "Visible to this account, but no Windows depot manifest was exposed."
                            : "Not listed in Steam branch metadata for this account.");

                    if (PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase))
                        details.Add("Steam marks this branch as password-protected; beta password entry is not implemented yet.");
                    else if (PasswordRequired.Equals("false", StringComparison.OrdinalIgnoreCase))
                        details.Add("Steam did not report a password requirement.");
                    else
                        details.Add("Steam did not expose password status.");

                    if (!string.IsNullOrWhiteSpace(BuildId))
                        details.Add($"Build ID: {BuildId}.");

                    if (!string.IsNullOrWhiteSpace(Description))
                        details.Add($"Description: {Description}.");
                }
                else if (!string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
                {
                    details.Add("Steam app-info metadata has not been captured for this option yet. Use Refresh Game Versions before downloading.");
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

        AddIfMissing(options, new BranchOption(SteamGameBranch.Beta, source: "fallback"));
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

    internal static string DropdownOptionLabels(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
        => string.Join(" | ", DropdownOptions(selectedBranch, discoveredBranches).Select(option => option.Label));

    internal static string SelectedOptionStatus(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        var option = DropdownOptions(selectedBranch, discoveredBranches)
            .FirstOrDefault(candidate => string.Equals(candidate.Branch, selectedBranch, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrWhiteSpace(option.Branch)
            ? "Steam app-info metadata is unavailable for the selected game version."
            : option.StatusText;
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
    }
}
