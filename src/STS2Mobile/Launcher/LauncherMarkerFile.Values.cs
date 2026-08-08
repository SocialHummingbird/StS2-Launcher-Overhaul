using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherMarkerFile
{
    internal static string ReadJoinedValues(
        string path,
        string prefix,
        string separator,
        string missingFileValue,
        string missingValuesValue,
        string readFailedValue = ReadFailedValue,
        int maxValues = int.MaxValue,
        Func<string, string> valueFormatter = null
    )
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

            return values.Count == 0
                ? missingValuesValue
                : string.Join(separator, values);
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
