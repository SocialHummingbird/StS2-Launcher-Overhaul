using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private static bool StringPropertyMatches(JsonElement root, string property, string expected)
        => !string.IsNullOrWhiteSpace(expected)
        && root.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
        && string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase);

    private static bool BoolPropertyMatches(JsonElement root, string property, bool expected)
        => root.TryGetProperty(property, out var value)
        && (value.ValueKind == JsonValueKind.True) == expected;

    private static bool StringArrayPropertyMatches(JsonElement root, string property, IReadOnlyList<string> expected)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            return expected == null || expected.Count == 0;

        var actual = value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        expected ??= Array.Empty<string>();
        return actual.Length == expected.Count
            && actual.Zip(expected, (left, right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase)).All(matches => matches);
    }

    private static bool StringDictionaryPropertyMatches(JsonElement root, string property, IReadOnlyDictionary<string, string> expected)
    {
        expected ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Object)
            return expected.Count == 0;

        var actual = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in value.EnumerateObject())
        {
            if (item.Value.ValueKind != JsonValueKind.String)
                return false;
            actual[item.Name] = item.Value.GetString() ?? string.Empty;
        }

        return actual.Count == expected.Count
            && expected.All(pair => actual.TryGetValue(pair.Key, out var actualValue)
                && string.Equals(actualValue, pair.Value, StringComparison.OrdinalIgnoreCase));
    }
}
