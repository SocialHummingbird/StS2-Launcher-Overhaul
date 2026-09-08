using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using STS2Mobile.Patches;

namespace LauncherUiPreview;

internal static class AssetPreloadRuntimeTest
{
    internal static async Task RunAsync(Node host, bool applyPatch)
    {
        var harmony = new Harmony("sts2launcher.tests.asset-preload");
        if (applyPatch) AndroidAssetPreloadPatches.ApplyForRuntime(harmony);
        var directory = Path.Combine(OS.GetUserDataDir(), "asset-preload-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var paths = new List<string>();
        try
        {
            // More than the desktop 128-item burst, including its serial VFX path.
            for (var i = 0; i < 160; i++)
            {
                var path = Path.Combine(directory, $"resource-{i}.tres").Replace('\\', '/');
                File.WriteAllText(path, "[gd_resource type=\"Gradient\" format=3]\n[resource]\n");
                paths.Add(path);
            }
            var vfxDirectory = Path.Combine(directory, "vfx");
            Directory.CreateDirectory(vfxDirectory);
            var vfxPath = Path.Combine(vfxDirectory, "probe.tscn").Replace('\\', '/');
            File.WriteAllText(vfxPath, "[gd_scene format=3]\n[node name=\"Probe\" type=\"Node\"]\n");
            paths.Add(vfxPath);

            var cache = new ConcurrentDictionary<string, Resource>();
            // A cache hit must consume no threaded request and still finish.
            cache[paths[0]] = ResourceLoader.Load(paths[0]);
            var session = new AssetLoadingSession("AndroidFrameBudgetProbe", paths, cache);
            var loading = Queue(session, "_loading");
            var finalizing = Queue(session, "_finalizing");
            var watch = Stopwatch.StartNew();
            var frames = 0;
            var maxFinalized = 0;
            var maxOutstanding = 0;
            while (!session.IsCompleted && watch.Elapsed < TimeSpan.FromSeconds(30))
            {
                var before = cache.Count;
                session.Process();
                var finalized = cache.Count - before;
                // The last finalized resource can also start/finish serial VFX;
                // our fixture has only one VFX, which finishes on a later frame.
                maxFinalized = Math.Max(maxFinalized, finalized);
                maxOutstanding = Math.Max(maxOutstanding, loading.Count + finalizing.Count);
                if (finalized > AndroidAssetPreloadPatches.MaximumFinalizationsPerFrame)
                    throw new InvalidOperationException($"Unbounded resource finalization: {finalized}");
                if (loading.Count + finalizing.Count > AndroidAssetPreloadPatches.MaximumInFlight)
                    throw new InvalidOperationException("Threaded resource backlog exceeded its bound.");
                frames++;
                await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            if (!session.IsCompleted || cache.Count != paths.Count || paths.Any(path => !cache.ContainsKey(path)))
                throw new InvalidOperationException($"Resources lost or stuck: {cache.Count}/{paths.Count}, completed={session.IsCompleted}");
            await session.WaitForCompletion();
            var cachedSession = new AssetLoadingSession("AndroidCachedBudgetProbe", paths, cache);
            var cachedFrames = 0;
            while (!cachedSession.IsCompleted && cachedFrames++ < 200)
            {
                cachedSession.Process();
                await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            if (!cachedSession.IsCompleted || cache.Count != paths.Count)
                throw new InvalidOperationException("A fully cached second session did not complete.");
            GD.Print($"ASSET_PRELOAD_PASS resources={cache.Count} frames={frames} maxFinalized={maxFinalized} "
                + $"maxOutstanding={maxOutstanding} cachedFrames={cachedFrames} elapsedMs={watch.ElapsedMilliseconds}");
            foreach (var resource in cache.Values) resource.Dispose();
        }
        finally
        {
            harmony.UnpatchAll(harmony.Id);
            // This unique test directory is the sole deletion target.
            Directory.Delete(directory, recursive: true);
        }
    }

    private static Queue<string> Queue(AssetLoadingSession session, string name)
        => (Queue<string>)typeof(AssetLoadingSession).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
}
