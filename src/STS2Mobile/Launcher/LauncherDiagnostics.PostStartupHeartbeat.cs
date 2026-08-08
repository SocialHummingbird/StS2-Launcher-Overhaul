using System;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal static void WritePostStartupHeartbeat(
        string phase,
        params string[] details
    )
    {
        var heartbeat = PostStartupHeartbeat(Godot.OS.GetDataDir());
        try
        {
            heartbeat.WriteAllText(BuildPostStartupHeartbeat(phase, details));
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Post-startup heartbeat failed: {ex.Message}");
        }
    }

    private static string BuildPostStartupHeartbeat(
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
                sb.AppendLine($"Selected branch: {SafePostStartupBranch()}");

                if (details == null)
                    return;

                foreach (var detail in details)
                    sb.AppendLine(CleanPostStartupValue(detail));
            }
        ).Build();
}
