using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace STS2Mobile.Launcher;

internal sealed partial class RuntimePackManifest
{
    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static int ReadInt(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
                return intValue;

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out intValue))
                return intValue;
        }

        return 0;
    }

    private static string[] ReadStringArray(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array)
                return value.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToArray();
        }

        return Array.Empty<string>();
    }

    private static bool HasProperty(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out _))
                return true;
        }

        return false;
    }

    private static IReadOnlyDictionary<string, string> ReadStringDictionary(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Object)
                continue;

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in value.EnumerateObject())
            {
                if (item.Value.ValueKind == JsonValueKind.String)
                    result[item.Name] = item.Value.GetString() ?? string.Empty;
            }
            return result;
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static bool ReadBool(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value))
            {
                if (value.ValueKind == JsonValueKind.True)
                    return true;
                if (value.ValueKind == JsonValueKind.False)
                    return false;
            }
        }

        return false;
    }
}