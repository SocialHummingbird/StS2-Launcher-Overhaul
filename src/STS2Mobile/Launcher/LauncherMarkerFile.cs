using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Globalization;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;
internal static partial class LauncherMarkerFile
{
    internal const string MissingFileValue = "<none>";
    internal const string MissingLineValue = "<missing>";
    internal const string ReadFailedValue = "<read failed>";
    internal static int CountLines(string path, string prefix)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return 0;
            var count = 0;
            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    count++;
            }

            return count;
        }
        catch
        {
            return 0;
        }
    }

    internal static bool HasConcreteValue(string value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("<", StringComparison.Ordinal);
    internal static TextSnapshot ReadSnapshot(string path)
    {
        if (!File.Exists(path))
            return new TextSnapshot(null, MissingFileValue, false);
        try
        {
            return new TextSnapshot(File.ReadAllLines(path), MissingLineValue, true);
        }
        catch
        {
            return new TextSnapshot(null, ReadFailedValue, true);
        }
    }

    internal sealed class TextSnapshot
    {
        private readonly string[] _lines;
        private readonly string _fallback;
        internal TextSnapshot(string[] lines, string fallback, bool present)
        {
            _lines = lines;
            _fallback = fallback;
            Present = present;
        }

        internal bool Present { get; }

        internal string ReadValue(string prefix) => _lines?.FirstOrDefault(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))is string match ? match[prefix.Length..].Trim() : _fallback;
    }

    internal static JsonSnapshot ReadJsonSnapshot(string path)
    {
        if (!File.Exists(path))
            return new JsonSnapshot(default, MissingFileValue, false);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new JsonSnapshot(default, ReadFailedValue, true);
            return new JsonSnapshot(document.RootElement.Clone(), MissingLineValue, true);
        }
        catch
        {
            return new JsonSnapshot(default, ReadFailedValue, true);
        }
    }

    internal sealed class JsonSnapshot
    {
        private readonly JsonElement _root;
        private readonly string _fallback;
        internal JsonSnapshot(JsonElement root, string fallback, bool present)
        {
            _root = root;
            _fallback = fallback;
            Present = present;
        }

        internal bool Present { get; }

        internal string ReadString(string property)
        {
            if (_root.ValueKind != JsonValueKind.Object || !_root.TryGetProperty(property, out var value))
                return _fallback;
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? MissingLineValue,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => value.ToString(),
                _ => "<unsupported>"
            };
        }

        internal string FailureMessages
        {
            get
            {
                if (_root.ValueKind != JsonValueKind.Object)
                    return _fallback;
                if (!_root.TryGetProperty("failureMessages", out var values) || values.ValueKind != JsonValueKind.Array)
                    return MissingLineValue;
                var messages = values.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString()).Where(value => !string.IsNullOrWhiteSpace(value)).Take(10).ToArray();
                return messages.Length == 0 ? MissingFileValue : string.Join(" | ", messages);
            }
        }
    }

    internal static int? ReadInt(string path, string prefix) => int.TryParse(ReadValue(path, prefix), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    internal static DateTime? ReadUtc(string path, string prefix = "UTC:")
    {
        var value = ReadOptionalValue(path, prefix);
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var utc) ? utc.ToUniversalTime() : null;
    }

    internal static bool ReadBoolFlag(string path, string prefix) => string.Equals(ReadOptionalValue(path, prefix), "true", StringComparison.OrdinalIgnoreCase);
    internal static string ReadJoinedValues(string path, string prefix, string separator, string missingFileValue, string missingValuesValue, string readFailedValue = ReadFailedValue, int maxValues = int.MaxValue, Func<string, string> valueFormatter = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return missingFileValue;
            var values = new List<string>();
            if (maxValues <= 0)
                return missingValuesValue;
            foreach (var line in File.ReadLines(path))
            {
                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                var value = line[prefix.Length..].Trim();
                values.Add(valueFormatter is null ? value : valueFormatter(value));
                if (values.Count >= maxValues)
                    break;
            }

            return values.Count == 0 ? missingValuesValue : string.Join(separator, values);
        }
        catch
        {
            return readFailedValue;
        }
    }

    internal static IReadOnlyList<string> ReadValues(string path, string prefix, int maxValues = int.MaxValue)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return Array.Empty<string>();
            var values = new List<string>();
            if (maxValues <= 0)
                return values;
            foreach (var line in File.ReadLines(path))
            {
                if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    continue;
                values.Add(line[prefix.Length..].Trim());
                if (values.Count >= maxValues)
                    break;
            }

            return values;
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
