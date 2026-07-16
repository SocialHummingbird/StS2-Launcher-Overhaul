using System;
using System.Linq;

namespace STS2Mobile;

internal static class DeprecatedSavePathMod
{
    internal static bool IsMatch(params string[] identities)
    {
        foreach (var identity in identities ?? Array.Empty<string>())
        {
            var normalized = new string((identity ?? "").Where(char.IsLetterOrDigit).ToArray());
            if (normalized.EndsWith("SavesMerger", StringComparison.OrdinalIgnoreCase)
                || normalized.EndsWith("UnifiedSavePath", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
