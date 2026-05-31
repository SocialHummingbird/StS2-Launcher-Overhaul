using System;
using System.IO;
using System.Text;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class LauncherStartupSceneSnapshot
{
    private const int MaxDepth = 6;
    private const int MaxChildrenPerNode = 40;

    internal static string Path => System.IO.Path.Combine(
        OS.GetDataDir(),
        LauncherStorageNames.StartupSceneSnapshot
    );

    internal static void Write(Node root, string reason)
    {
        try
        {
            File.WriteAllText(Path, Build(root, reason));
            PatchHelper.Log($"Startup scene snapshot written: {reason}");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Startup scene snapshot failed: {ex.Message}");
        }
    }

    private static string Build(Node root, string reason)
    {
        var sb = new StringBuilder();
        sb.AppendLine("STS2 startup scene snapshot");
        sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Reason: {reason}");
        sb.AppendLine();
        AppendNode(sb, root, depth: 0);
        return sb.ToString();
    }

    private static void AppendNode(
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

        if (depth >= MaxDepth)
            return;

        var childCount = node.GetChildCount();
        var limit = Math.Min(childCount, MaxChildrenPerNode);
        for (var i = 0; i < limit; i++)
            AppendNode(sb, node.GetChild(i), depth + 1);

        if (childCount > limit)
        {
            sb.Append(indent)
                .Append("  ... ")
                .Append(childCount - limit)
                .AppendLine(" more children");
        }
    }
}
