using System.IO;

namespace STS2Mobile.Launcher;

internal static partial class LauncherCloudSyncEvidence
{
    internal const string LastManualPullMarkerFileName = "last_manual_cloud_pull.txt";
    internal const string LastManualPushMarkerFileName = "last_manual_cloud_push.txt";
    internal const string LastManualPushBlockedMarkerFileName = "last_manual_cloud_push_blocked.txt";

    internal static string LastManualPullMarkerPath(string dataDir)
        => Path.Combine(dataDir, LastManualPullMarkerFileName);

    internal static string LastManualPushMarkerPath(string dataDir)
        => Path.Combine(dataDir, LastManualPushMarkerFileName);

    internal static string LastManualPushBlockedMarkerPath(string dataDir)
        => Path.Combine(dataDir, LastManualPushBlockedMarkerFileName);
}
