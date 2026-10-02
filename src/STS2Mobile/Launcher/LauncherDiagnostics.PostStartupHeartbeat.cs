using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal static void WritePostStartupHeartbeat(
        string phase,
        params string[] details
    )
        => WritePostStartupHeartbeatForLaunch(
            Godot.OS.GetDataDir(), SafePostStartupBranch(), phase, details);

    // Safe for the existing diagnostic timer: every engine/preference value
    // was captured by its caller before leaving the Godot thread.
    internal static void WritePostStartupHeartbeatForLaunch(
        string dataDir,
        string branch,
        string phase,
        params string[] details
    )
    {
        try
        {
            PostStartupHeartbeat(dataDir).WriteAllText(BuildPostStartupHeartbeat(branch, phase, details));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Post-startup heartbeat failed: {ex.Message}");
        }
    }

    private static string BuildPostStartupHeartbeat(
        string branch,
        string phase,
        string[] details
    )
        => CreateTimestampedText(
            "STS2 post-startup heartbeat",
            "UTC",
            sb =>
            {
                sb.AppendLine($"Phase: {CleanPostStartupValue(phase)}");
                sb.AppendLine($"Elapsed ms: {LauncherLaunchMarkers.ElapsedMilliseconds}");
                sb.AppendLine($"Selected branch: {CleanPostStartupValue(branch)}");

                if (details == null)
                    return;

                foreach (var detail in details)
                    sb.AppendLine(CleanPostStartupValue(detail));
            }
        ).Build();
}
