using System.Globalization;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
    internal static string LastManualPushUtc(string dataDir)
    {
        var utc = ReadUtc(LastManualPushMarkerPath(dataDir));
        return utc.HasValue
            ? utc.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
            : "<none>";
    }

    internal static bool LastManualPushUtcParseable(string dataDir)
        => ReadUtc(LastManualPushMarkerPath(dataDir)).HasValue;
}
