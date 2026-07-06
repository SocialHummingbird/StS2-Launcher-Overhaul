using System;
using System.Collections.Generic;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherMarkerFile
{
    internal static string ReadValue(
        string path,
        string prefix,
        string missingFileValue = MissingFileValue,
        string missingLineValue = MissingLineValue,
        string readFailedValue = ReadFailedValue
    )
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return missingFileValue;

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line[prefix.Length..].Trim();
            }
        }
        catch
        {
            return readFailedValue;
        }

        return missingLineValue;
    }

    internal static string ReadOptionalValue(string path, string prefix)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            foreach (var line in File.ReadLines(path))
            {
                if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return line[prefix.Length..].Trim();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    internal static IReadOnlyDictionary<string, string> ReadOptionalValues(
        string path,
        params string[] prefixes
    )
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path) || prefixes == null)
                return values;

            foreach (var line in File.ReadLines(path))
            {
                foreach (var prefix in prefixes)
                {
                    if (string.IsNullOrWhiteSpace(prefix)
                        || values.ContainsKey(prefix)
                        || !line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        continue;

                    values[prefix] = line[prefix.Length..].Trim();
                    if (values.Count >= prefixes.Length)
                        return values;
                }
            }
        }
        catch
        {
            values.Clear();
        }

        return values;
    }
}
