using System;
using System.Collections.Generic;
using System.Globalization;

namespace STS2Mobile.Steam;

internal readonly struct SteamBranchAvailabilityMarkerRow
{
    private readonly Dictionary<string, string> _metadata;

    private SteamBranchAvailabilityMarkerRow(
        string branch,
        Dictionary<string, string> metadata,
        string rawValue
    )
    {
        Branch = branch ?? "";
        _metadata = metadata;
        RawValue = rawValue ?? "";
    }

    internal string Branch { get; }
    internal string RawValue { get; }
    internal bool IsEmpty => string.IsNullOrWhiteSpace(Branch);
    internal bool MetadataVisible => BoolValue(SteamBranchAvailabilityMarkerFields.MetadataVisibleKey);
    internal int WindowsManifestDepotCount => IntValue(SteamBranchAvailabilityMarkerFields.WindowsManifestDepotsKey);
    internal bool HasWindowsManifestDepotCount => !string.IsNullOrWhiteSpace(Value(SteamBranchAvailabilityMarkerFields.WindowsManifestDepotsKey));
    internal string PasswordRequired => Value(SteamBranchAvailabilityMarkerFields.PasswordRequiredKey);
    internal string BuildId => Value(SteamBranchAvailabilityMarkerFields.BuildIdKey);
    internal string Description => Value(SteamBranchAvailabilityMarkerFields.DescriptionKey);
    internal bool PasswordProtected => PasswordRequired.Equals("true", StringComparison.OrdinalIgnoreCase);
    internal bool DownloadableOrUnspecified => !HasWindowsManifestDepotCount || WindowsManifestDepotCount > 0;

    internal static SteamBranchAvailabilityMarkerRow Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new("", new(StringComparer.OrdinalIgnoreCase), value ?? "");

        value = value.Trim();
        var nameEnd = value.IndexOf(" [", StringComparison.Ordinal);
        var branch = (nameEnd > 0 ? value[..nameEnd] : value).Trim();
        var metadata = nameEnd > 0 && value.EndsWith("]", StringComparison.Ordinal)
            ? ParseMetadata(value[(nameEnd + 2)..^1])
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return new(branch, metadata, value);
    }

    internal bool BranchMatches(string branch)
    {
        if (string.IsNullOrWhiteSpace(Branch) || string.IsNullOrWhiteSpace(branch))
            return false;

        return string.Equals(
            SteamGameBranch.Normalize(Branch),
            SteamGameBranch.Normalize(branch),
            StringComparison.OrdinalIgnoreCase
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

    private bool BoolValue(string key)
        => Value(key).Equals("true", StringComparison.OrdinalIgnoreCase);

    private int IntValue(string key)
        => int.TryParse(Value(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private string Value(string key)
        => _metadata != null && _metadata.TryGetValue(key, out var value) ? value : "";
}
