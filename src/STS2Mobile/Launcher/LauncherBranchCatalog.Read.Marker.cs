using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
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

    private static bool BoolValue(Dictionary<string, string> values, string key)
        => Value(values, key).Equals("true", StringComparison.OrdinalIgnoreCase);

    private static int IntValue(Dictionary<string, string> values, string key)
        => int.TryParse(Value(values, key), out var value) ? value : 0;

    private static string Value(Dictionary<string, string> values, string key)
        => values.TryGetValue(key, out var value) ? value : "";
}
