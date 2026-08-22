using System;
using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherMarkerFile
{
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

    internal static bool HasConcreteValue(string value)
        => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("<", StringComparison.Ordinal);
}
