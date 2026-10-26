using System;

namespace STS2Mobile.Patches;

internal static class AndroidSavePathNormalizer
{
    internal static string Normalize(string path, string defaultAccountPrefix, bool hasOtherAccountOverride = false)
    {
        if (hasOtherAccountOverride) return path;
        if (string.Equals(path, defaultAccountPrefix, StringComparison.Ordinal)) return "user://";
        var childPrefix = defaultAccountPrefix + "/";
        return path?.StartsWith(childPrefix, StringComparison.Ordinal) == true
            ? "user://" + path.Substring(childPrefix.Length)
            : path;
    }
}
