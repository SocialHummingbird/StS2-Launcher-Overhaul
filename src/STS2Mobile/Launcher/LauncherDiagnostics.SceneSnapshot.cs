using System;
using System.Text;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherDiagnostics
{
    private const int MaxSceneSnapshotChildrenPerNode = 40;
    private const int MaxSceneSnapshotDepth = 6;

    internal static void WriteStartupSceneSnapshot(Node root, string reason)
    {
        var snapshot = StartupSceneSnapshot(OS.GetDataDir());
        try
        {
            snapshot.WriteAllText(BuildStartupSceneSnapshot(root, reason));
            PatchHelper.Log($"Startup scene snapshot written: {reason}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup scene snapshot failed: {ex.Message}");
        }
    }

    private static void AppendSceneNode(
        StringBuilder sb,
        Node node,
        int depth
    )
    {
        if (node == null)
            return;

        var indent = new string(' ', depth * 2);
        sb.Append(indent)
            .Append(node.GetType().FullName)
            .Append(" name=")
            .Append(node.Name)
            .Append(" children=")
            .Append(node.GetChildCount())
            .AppendLine();

        if (depth >= MaxSceneSnapshotDepth)
            return;

        var childCount = node.GetChildCount();
        var limit = Math.Min(childCount, MaxSceneSnapshotChildrenPerNode);
        for (var i = 0; i < limit; i++)
            AppendSceneNode(sb, node.GetChild(i), depth + 1);

        if (childCount > limit)
        {
            sb.Append(indent)
                .Append("  ... ")
                .Append(childCount - limit)
                .AppendLine(" more children");
        }
    }

    private static string BuildStartupSceneSnapshot(Node root, string reason)
    {
        var sb = new StringBuilder();
        sb.AppendLine("STS2 startup scene snapshot");
        sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Reason: {reason}");
        sb.AppendLine();
        AppendSceneNode(sb, root, depth: 0);
        return sb.ToString();
    }
}
