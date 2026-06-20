using System;
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
}
