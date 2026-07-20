using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class LauncherGameStartupRecovery
{
    private static readonly PostStartupDiagnosticsScheduleGate PostStartupScheduleGate =
        new();

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
        => WritePostStartupEvidence(
            game,
            gameNode,
            phase,
            writeFullTrace: true,
            details
        );

    private static void WriteSuccessfulPostStartupEvidence(
        object game,
        Node gameNode,
        string phase,
        params string[] details
    )
        => WritePostStartupEvidence(
            game,
            gameNode,
            phase,
            PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(
                PostStartupDiagnosticsSettings.DetailedTraceEnabled(),
                failureOrRecovery: false
            ),
            details
        );

    private static void WritePostStartupEvidence(
        object game,
        Node gameNode,
        string phase,
        bool writeFullTrace,
        params string[] details
    )
    {
        try
        {
            var scene = InspectCurrentScene(game);
            var mergedDetails = MergePostStartupTraceDetails(scene, details);
            LauncherDiagnostics.WritePostStartupHeartbeat(phase, mergedDetails);
            if (writeFullTrace)
            {
                LauncherDiagnostics.WritePostStartupTrace(
                    gameNode,
                    phase,
                    mergedDetails
                );
            }
            PatchHelper.Log(
                $"[PostStartupEvidence] phase={phase} mainMenu={scene.IsMainMenu} "
                + $"scene={scene.SceneName ?? "<none>"} fullTrace={writeFullTrace}"
            );
        }
        catch (Exception ex)
        {
            LauncherDiagnostics.WritePostStartupHeartbeat(
                phase,
                $"Evidence failure: {ex.GetType().Name}: {ex.Message}"
            );
            if (writeFullTrace)
            {
                LauncherDiagnostics.WritePostStartupTrace(
                    gameNode,
                    phase,
                    $"Trace failure: {ex.GetType().Name}: {ex.Message}"
                );
            }
            PatchHelper.Log($"[PostStartupEvidence] failed: {ex}");
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

    private static void SchedulePostStartupDiagnostics(object game, Node gameNode)
    {
        if (!PostStartupScheduleGate.TrySchedule())
        {
            PatchHelper.Log(
                "Post-startup diagnostics already scheduled; duplicate request ignored"
            );
            return;
        }

        bool detailedTraceEnabled =
            PostStartupDiagnosticsSettings.DetailedTraceEnabled();
        PatchHelper.Log(
            detailedTraceEnabled
                ? "Detailed timed post-startup scene traces enabled by explicit opt-in"
                : "Detailed timed post-startup scene traces disabled; lightweight probes remain active"
        );
        _ = RunScheduledPostStartupDiagnosticsAsync(
            game,
            gameNode,
            detailedTraceEnabled
        );
    }

    private static async Task RunScheduledPostStartupDiagnosticsAsync(
        object game,
        Node gameNode,
        bool detailedTraceEnabled
    )
    {
        if (!GodotObject.IsInstanceValid(gameNode))
        {
            PostStartupScheduleGate.Stop();
            return;
        }

        using var cancellation = new CancellationTokenSource();
        void CancelForTreeExit()
        {
            PostStartupScheduleGate.Stop();
            cancellation.Cancel();
        }

        var subscribed = false;
        try
        {
            gameNode.TreeExiting += CancelForTreeExit;
            subscribed = true;
            await Task.WhenAll(
                RunPostStartupProbeAsync(
                    game,
                    gameNode,
                    detailedTraceEnabled,
                    cancellation.Token
                ),
                RunPostStartupHeartbeatAsync(game, cancellation.Token)
            );
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            PatchHelper.Log(
                "Post-startup diagnostics cancelled during game-scene teardown"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Post-startup diagnostics scheduling failed: {ex}");
        }
        finally
        {
            PostStartupScheduleGate.Stop();
            if (subscribed && GodotObject.IsInstanceValid(gameNode))
            {
                try
                {
                    gameNode.TreeExiting -= CancelForTreeExit;
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(
                        $"Post-startup teardown handler cleanup failed: {ex.Message}"
                    );
                }
            }
        }
    }

    private static async Task RunPostStartupProbeAsync(
        object game,
        Node gameNode,
        bool detailedTraceEnabled,
        CancellationToken cancellationToken
    )
    {
        var elapsed = 0;
        foreach (var target in PostStartupProbeTargetsMs)
        {
            await Task.Delay(
                Math.Max(0, target - elapsed),
                cancellationToken
            );
            elapsed = target;
            var phase = $"post-startup alive at {target}ms";
            if (detailedTraceEnabled)
                WritePostStartupTrace(game, gameNode, phase);
            else
                WritePostStartupHeartbeat(game, phase);
        }
    }

    private static async Task RunPostStartupHeartbeatAsync(
        object game,
        CancellationToken cancellationToken
    )
    {
        var elapsed = 0;
        foreach (var target in PostStartupHeartbeatTargetsMs)
        {
            await Task.Delay(
                Math.Max(0, target - elapsed),
                cancellationToken
            );
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
