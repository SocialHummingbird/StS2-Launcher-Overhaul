using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void PostStartupHeartbeatSurvivesBlockedCaller()
    {
        var schedule = typeof(LauncherGameStartupRecovery).GetMethod("RunPostStartupHeartbeatAsync",
            BindingFlags.NonPublic | BindingFlags.Static, null,
            new[] { typeof(Action<string>), typeof(CancellationToken), typeof(int[]) }, null);
        True(schedule != null, "The existing heartbeat schedule needs a callback boundary that does not inspect live Godot objects.");
        var callerThread = Environment.CurrentManagedThreadId;
        var writerThread = callerThread;
        var phases = new List<string>();
        using var cancellation = new CancellationTokenSource();
        using var context = new PreloadCallerContext();
        var task = (Task)schedule!.Invoke(null, new object[] {
            (Action<string>)(phase => { writerThread = Environment.CurrentManagedThreadId; phases.Add(phase); }),
            cancellation.Token, new[] { 10, 20 }
        })!;
        try
        {
            True(task.Wait(TimeSpan.FromSeconds(2)),
                "Lightweight evidence must finish even when the caller's Godot synchronization context is not pumped.");
            Equal(2, phases.Count, "All scheduled heartbeats must be retained.");
            NotEqual(callerThread, writerThread, "Heartbeat persistence belongs on the worker, not the blocked game thread.");
        }
        finally
        {
            cancellation.Cancel();
            try { context.RunUntilCompleted(task); }
            catch (OperationCanceledException) { }
        }
    }

    private static void PostStartupHeartbeatStopsAtTeardown()
    {
        var schedule = typeof(LauncherGameStartupRecovery).GetMethod("RunPostStartupHeartbeatAsync",
            BindingFlags.NonPublic | BindingFlags.Static, null,
            new[] { typeof(Action<string>), typeof(CancellationToken), typeof(int[]) }, null);
        True(schedule != null, "The existing heartbeat callback schedule must be available.");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var writes = 0;
        var task = (Task)schedule!.Invoke(null, new object[] {
            (Action<string>)(_ => writes++), cancellation.Token, new[] { 10 }
        })!;
        try { task.GetAwaiter().GetResult(); throw new Exception("Cancelled diagnostics continued."); }
        catch (OperationCanceledException) { }
        Equal(0, writes, "Scene teardown must stop pending heartbeat persistence.");
    }

    private static void PostStartupHeartbeatUsesCapturedLaunch()
    {
        var writer = typeof(LauncherDiagnostics).GetMethod("WritePostStartupHeartbeatForLaunch",
            BindingFlags.NonPublic | BindingFlags.Static);
        True(writer != null, "Worker evidence needs captured storage and branch values instead of native Godot reads.");
        var directory = Path.Combine(Path.GetTempPath(), "sts2-heartbeat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            Task.Run(() => writer!.Invoke(null, new object[] {
                directory, "beta", "post-startup heartbeat at 1000ms",
                new[] { "Attempt: captured-attempt", "Scene sampled at elapsed ms: 123", "Current scene: NMainMenu" }
            })).GetAwaiter().GetResult();
            var report = File.ReadAllText(Path.Combine(directory, LauncherStorageNames.PostStartupHeartbeat));
            True(report.Contains("Selected branch: beta"), "Heartbeat must use the launch's captured branch.");
            True(report.Contains("Attempt: captured-attempt"), "Heartbeat must retain launch provenance.");
            True(report.Contains("Scene sampled at elapsed ms: 123"), "A worker timer must distinguish stale scene evidence from live inspection.");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
