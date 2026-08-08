using System;
using System.Text;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    internal static void WritePostStartupTrace(
        Node root,
        string phase,
        params string[] details
    )
    {
        var trace = PostStartupTrace(OS.GetDataDir());
        try
        {
            trace.WriteAllText(BuildPostStartupTrace(root, phase, details));
            PatchHelper.Log($"Post-startup trace written: {phase}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Post-startup trace failed: {ex.Message}");
        }
    }

    private static string BuildPostStartupTrace(
        Node root,
        string phase,
        string[] details
    )
        => CreateTimestampedText(
            "STS2 post-startup trace",
            "UTC",
            sb =>
            {
                AppendPostStartupHeader(sb, phase, details);
                sb.AppendLine();
                sb.AppendLine("Scene tree:");
                AppendSceneNode(sb, root, depth: 0);
            }
        ).Build();

    private static void AppendPostStartupHeader(
        StringBuilder sb,
        string phase,
        string[] details
    )
    {
        sb.AppendLine($"Phase: {CleanPostStartupValue(phase)}");
        sb.AppendLine($"Elapsed ms: {LauncherLaunchMarkers.ElapsedMilliseconds}");
        sb.AppendLine($"Selected branch: {SafePostStartupBranch()}");

        if (details == null)
            return;

        foreach (var detail in details)
            sb.AppendLine(CleanPostStartupValue(detail));
    }

    private static string CleanPostStartupValue(string value)
        => string.IsNullOrWhiteSpace(value)
            ? "<none>"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    private static string SafePostStartupBranch()
    {
        try
        {
            return LauncherPreferences.ReadGameBranch();
        }
        catch (Exception ex)
        {
            return $"<unavailable:{ex.GetType().Name}>";
        }
    }
}
