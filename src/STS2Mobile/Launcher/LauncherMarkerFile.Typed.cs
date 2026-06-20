using System;
using System.Globalization;

namespace STS2Mobile.Launcher;

internal static partial class LauncherMarkerFile
{
    internal static int? ReadInt(string path, string prefix)
        => int.TryParse(
            ReadValue(path, prefix),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value
        )
            ? value
            : null;

    internal static DateTime? ReadUtc(string path, string prefix = "UTC:")
    {
        var value = ReadOptionalValue(path, prefix);
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out var utc
        )
            ? utc.ToUniversalTime()
            : null;
    }

    internal static bool UtcParseable(string path, string prefix = "UTC:")
        => DateTime.TryParse(
            ReadValue(path, prefix),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal,
            out _
        );

    internal static bool ReadBoolFlag(string path, string prefix)
        => string.Equals(
            ReadOptionalValue(path, prefix),
            "true",
            StringComparison.OrdinalIgnoreCase
        );
}
