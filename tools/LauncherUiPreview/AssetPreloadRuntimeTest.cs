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
        if (applyPatch)
        {
            VerifyOwnershipInspection(harmony);
            foreach (var ownership in AndroidAssetPreloadPatches.DescribeRuntimePatchOwnership())
            {
                GD.Print(ownership);
                if (!ownership.Contains("ownedPrefix=True") || !ownership.Contains(harmony.Id))
                    throw new InvalidOperationException("The real game target lost its preload prefix: " + ownership);
            }
        }
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
            await VerifySynchronousFallbacksAsync(host, directory);
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

    private static int _prefixFactoryCalls;

    private static MethodInfo OwnershipProbePrefixFactory(MethodBase target)
    {
        _prefixFactoryCalls++;
        return typeof(AssetPreloadRuntimeTest).GetMethod(nameof(OwnershipProbePrefix),
            BindingFlags.Static | BindingFlags.NonPublic)!;
    }

    private static void OwnershipProbePrefix() { }

    private static void VerifyOwnershipInspection(Harmony preloadHarmony)
    {
        var factoryHarmony = new Harmony("sts2launcher.tests.asset-owner-factory");
        var target = typeof(AssetLoadingSession).GetMethod("CheckLoadingStatus",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        try
        {
            factoryHarmony.Patch(target, prefix: new HarmonyMethod(
                typeof(AssetPreloadRuntimeTest).GetMethod(nameof(OwnershipProbePrefixFactory),
                    BindingFlags.Static | BindingFlags.NonPublic)!)
                { priority = Priority.First, before = new[] { preloadHarmony.Id } });
            var callsAfterPatching = _prefixFactoryCalls;
            if (callsAfterPatching == 0)
                throw new InvalidOperationException("The factory fixture did not execute while applying its real Harmony patch.");
            var report = AndroidAssetPreloadPatches.DescribeRuntimePatchOwnership();
            if (_prefixFactoryCalls != callsAfterPatching)
                throw new InvalidOperationException("Ownership inspection re-executed a mod patch factory.");
            var status = report.Single(line => line.Contains("target=CheckLoadingStatus"));
            if (!status.Contains(factoryHarmony.Id) || !status.Contains("priority=800")
                || !status.Contains("before=[" + preloadHarmony.Id + "]"))
                throw new InvalidOperationException("Ownership inspection lost the factory's owner or ordering metadata.");
            GD.Print("ASSET_OWNERSHIP_PASS factoryCallsUnchanged=true ownerAndConstraintsRetained=true");
        }
        finally { factoryHarmony.UnpatchAll(factoryHarmony.Id); }
    }

    private static async Task VerifySynchronousFallbacksAsync(Node host, string directory)
    {
        var assetCache = new AssetCache();
        var cache = (ConcurrentDictionary<string, Resource>)typeof(AssetCache)
            .GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(assetCache)!;
        var session = assetCache.CreateSession("FailedThreadedRequests", Array.Empty<string>());
        var loading = Queue(session, "_loading");
        for (var i = 0; i < 8; i++)
        {
            var nestedScene = i % 2 == 1;
            var path = Path.Combine(directory, $"fallback-{i}." + (nestedScene ? "tscn" : "tres")).Replace('\\', '/');
            File.WriteAllText(path, nestedScene
                ? "[gd_scene load_steps=3 format=3]\n[sub_resource type=\"StandardMaterial3D\" id=\"Material\"]\n"
                    + "[sub_resource type=\"BoxMesh\" id=\"Mesh\"]\nmaterial = SubResource(\"Material\")\n"
                    + "[node name=\"MeshProbe\" type=\"MeshInstance3D\"]\nmesh = SubResource(\"Mesh\")\n"
                : "[gd_resource type=\"Gradient\" format=3]\n[resource]\n");
            // A valid resource without a threaded request yields InvalidResource,
            // exercising the real game's synchronous fallback rather than a mock.
            loading.Enqueue(path);
        }
        var missing = Path.Combine(directory, "missing-fallback.tres").Replace('\\', '/');
        loading.Enqueue(missing);
        var frames = 0;
        try
        {
            while (!session.IsCompleted && frames++ < 30)
            {
                var before = cache.Count;
                session.Process();
                if (cache.Count - before > 1)
                    throw new InvalidOperationException($"Synchronous fallback burst exceeded one resource per pass: {cache.Count - before}");
                await host.ToSignal(host.GetTree(), SceneTree.SignalName.ProcessFrame);
            }
            if (!session.IsCompleted || cache.Count != 8)
                throw new InvalidOperationException($"Synchronous fallback lost resources: {cache.Count}/8 completed={session.IsCompleted}");
            await session.WaitForCompletion();
            var failed = (HashSet<string>)typeof(AssetCache)
                .GetField("_failedAssets", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(assetCache)!;
            var totalLoaded = (int)typeof(AssetLoadingSession)
                .GetField("_totalLoaded", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(session)!;
            if (!failed.SetEquals(new[] { missing }) || totalLoaded != 8)
                throw new InvalidOperationException($"Fallback accounting changed: failed={failed.Count}, loaded={totalLoaded}");
            try
            {
                assetCache.GetAsset<Resource>(missing);
                throw new InvalidOperationException("A failed asset was silently accepted by the game cache.");
            }
            catch (Exception ex) when (ex.GetType().Name == "AssetLoadException") { }
            GD.Print($"ASSET_FALLBACK_PASS resources={cache.Count} failed={failed.Count} nestedScenes=4 frames={frames}");
        }
        finally
        {
            foreach (var resource in cache.Values) resource.Dispose();
        }
    }
}
