using System;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private static readonly int[] PostStartupProbeTargetsMs =
    {
        1_000,
        3_000,
        10_000,
        30_000,
    };

    private static readonly int[] PostStartupHeartbeatTargetsMs =
    {
        60_000,
        120_000,
        180_000,
        300_000,
    };

    private static void WritePostStartupTrace(
        object game,
        Node gameNode,
        string phase,
        params string[] details
    )
    {
        try
        {
            var scene = InspectCurrentScene(game);
            var mergedDetails = MergePostStartupTraceDetails(scene, details);
            LauncherDiagnostics.WritePostStartupHeartbeat(phase, mergedDetails);
            LauncherDiagnostics.WritePostStartupTrace(
                gameNode,
                phase,
                mergedDetails
            );
            PatchHelper.Log(
                $"[PostStartupTrace] phase={phase} mainMenu={scene.IsMainMenu} scene={scene.SceneName ?? "<none>"}"
            );
        }
        catch (Exception ex)
        {
            LauncherDiagnostics.WritePostStartupTrace(
                gameNode,
                phase,
                $"Trace failure: {ex.GetType().Name}: {ex.Message}"
            );
            PatchHelper.Log($"[PostStartupTrace] failed: {ex}");
        }
    }

    private static string[] MergePostStartupTraceDetails(
        CurrentSceneInspection scene,
        string[] details
    )
    {
        var prefix = new[]
        {
            $"Current scene: {scene.SceneName ?? "<none>"}",
            $"Current scene is main menu: {scene.IsMainMenu}",
        };

        if (details == null || details.Length == 0)
            return prefix;

        var merged = new string[prefix.Length + details.Length];
        Array.Copy(prefix, merged, prefix.Length);
        Array.Copy(details, 0, merged, prefix.Length, details.Length);
        return merged;
    }

    private static void SchedulePostStartupTrace(object game, Node gameNode)
    {
        _ = RunPostStartupTraceAsync(game, gameNode);
        _ = RunPostStartupHeartbeatAsync(game);
    }

    private static async Task RunPostStartupTraceAsync(object game, Node gameNode)
    {
        var elapsed = 0;
        foreach (var target in PostStartupProbeTargetsMs)
        {
            await Task.Delay(Math.Max(0, target - elapsed));
            elapsed = target;
            WritePostStartupTrace(
                game,
                gameNode,
                $"post-startup alive at {target}ms"
            );
        }
    }

    private static async Task RunPostStartupHeartbeatAsync(object game)
    {
        var elapsed = 0;
        foreach (var target in PostStartupHeartbeatTargetsMs)
        {
            await Task.Delay(Math.Max(0, target - elapsed));
            elapsed = target;
            WritePostStartupHeartbeat(game, $"post-startup heartbeat at {target}ms");
        }
    }

    private static void WritePostStartupHeartbeat(
        object game,
        string phase,
        params string[] details
    )
    {
        try
        {
            var scene = InspectCurrentScene(game);
            LauncherDiagnostics.WritePostStartupHeartbeat(
                phase,
                MergePostStartupTraceDetails(scene, details)
            );
            PatchHelper.Log(
                $"[PostStartupHeartbeat] phase={phase} mainMenu={scene.IsMainMenu} scene={scene.SceneName ?? "<none>"}"
            );
        }
        catch (Exception ex)
        {
            LauncherDiagnostics.WritePostStartupHeartbeat(
                phase,
                $"Heartbeat failure: {ex.GetType().Name}: {ex.Message}"
            );
            PatchHelper.Log($"[PostStartupHeartbeat] failed: {ex}");
        }
    }
}
