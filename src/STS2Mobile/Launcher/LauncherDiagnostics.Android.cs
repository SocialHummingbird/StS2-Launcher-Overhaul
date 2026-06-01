using System;
using System.Text;
using STS2Mobile;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private static void AppendAndroidBridgeSection(StringBuilder sb, string dataDir)
    {
        try
        {
            AppendAndroidBridgeMetadata(sb, dataDir);
        }
        catch (Exception ex)
        {
            sb.AppendLine(AndroidBridgeFailed(ex));
        }

        AppendLogcatTail(
            sb,
            AndroidLogcatTail,
            AndroidBridgeTailLines,
            leadingBlank: true
        );
    }

    private static void AppendAndroidBridgeMetadata(StringBuilder sb, string dataDir)
    {
        sb.AppendLine(
            AndroidAppVersion(
                AndroidGodotAppBridge.GetVersionName()
            )
        );
        sb.AppendLine(
            ExternalFilesDir(
                AndroidGodotAppBridge.GetExternalFilesDirPath()
            )
        );
        sb.AppendLine(
            UsableDataBytes(
                AndroidGodotAppBridge.GetUsableSpaceBytes(dataDir)
            )
        );
    }
}
