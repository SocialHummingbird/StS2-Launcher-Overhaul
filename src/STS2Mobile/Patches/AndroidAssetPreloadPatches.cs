using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using STS2Mobile.Launcher;

namespace STS2Mobile.Patches;

// NAssetLoader processes sessions on the render thread, including the Common
// session queued just after the menu appears. Desktop queues request 128 loads
// and finalize the entire ready backlog in one frame. Keep the game's queues,
// status/failure handling, VFX ordering and completion, but bound those bursts.
internal static class AndroidAssetPreloadPatches
{
    internal const int MaximumInFlight = 8;
    internal const int MaximumFinalizationsPerFrame = 4;
    internal const int MaximumSynchronousFallbacksPerPass = 1;
    private const int MaximumRequestCandidatesPerFrame = 64;
    private const double WorkBudgetMilliseconds = 4;
    // Runtime preparation publicizes these members; the desktop reference
    // retains private visibility. Both paths use the same shape validation.
    private const BindingFlags TargetMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Apply(Harmony harmony)
    {
        if (OperatingSystem.IsAndroid()) ApplyForRuntime(harmony);
    }

    // Also exercised with real threaded resources by the desktop Godot probe.
    internal static void ApplyForRuntime(Harmony harmony)
    {
        var type = typeof(AssetLoadingSession);
        RequireField(type, "_loading", typeof(Queue<string>));
        RequireField(type, "_toLoad", typeof(Queue<string>));
        RequireField(type, "_finalizing", typeof(Queue<string>));
        RequireField(type, "_cache", typeof(ConcurrentDictionary<string, Resource>));
        RequireField(type, "_totalLoaded", typeof(int));
        RequireField(type, "_assetCache", typeof(AssetCache));
        var request = RequireMethod(type, "ProcessLoadingQueue");
        var finalize = RequireMethod(type, "FinalizeLoading");
        var status = RequireMethod(type, "CheckLoadingStatus");
        harmony.Patch(request, prefix: new HarmonyMethod(
            PatchHelper.Method(typeof(AndroidAssetPreloadPatches), nameof(ProcessLoadingQueuePrefix))));
        harmony.Patch(finalize, prefix: new HarmonyMethod(
            PatchHelper.Method(typeof(AndroidAssetPreloadPatches), nameof(FinalizeLoadingPrefix))));
        harmony.Patch(status, prefix: new HarmonyMethod(
            PatchHelper.Method(typeof(AndroidAssetPreloadPatches), nameof(CheckLoadingStatusPrefix))));
        PatchHelper.Log($"[AssetPreload] Frame budgets installed: inFlight={MaximumInFlight} "
            + $"finalize={MaximumFinalizationsPerFrame} syncFallback={MaximumSynchronousFallbacksPerPass} budgetMs={WorkBudgetMilliseconds}");
    }

    // Mods can change Harmony ownership after the startup patch report. Inspect
    // declarations and ordering constraints only: resolving sorted executable
    // methods can invoke third-party patch factories again.
    internal static string[] DescribeRuntimePatchOwnership()
    {
        return new[] { "ProcessLoadingQueue", "FinalizeLoading", "CheckLoadingStatus" }
            .Select(name =>
            {
                var target = RequireMethod(typeof(AssetLoadingSession), name);
                var info = Harmony.GetPatchInfo(target);
                if (info == null)
                    return $"[AssetPreload] target={name} ownedPrefix=False patches=<none>";
                var ownedPrefix = info.Prefixes.Any(patch =>
                    patch.PatchMethod == PatchHelper.Method(typeof(AndroidAssetPreloadPatches), name + "Prefix"));
                string Declared(string kind, IEnumerable<Patch> patches)
                {
                    return kind + "=[" + string.Join(", ", patches.OrderBy(patch => patch.index).Select(patch =>
                        $"owner={patch.owner} method={patch.PatchMethod.DeclaringType?.FullName}.{patch.PatchMethod.Name} "
                        + $"index={patch.index} priority={patch.priority} before=[{string.Join("/", patch.before)}] after=[{string.Join("/", patch.after)}]")) + "]";
                }
                return $"[AssetPreload] target={name} ownedPrefix={ownedPrefix} order=declarations-only factoriesNotEvaluated=True "
                    + Declared("prefixes", info.Prefixes) + " " + Declared("postfixes", info.Postfixes)
                    + " " + Declared("transpilers", info.Transpilers) + " " + Declared("finalizers", info.Finalizers);
            }).ToArray();
    }

    private static bool ProcessLoadingQueuePrefix(Queue<string> ____loading,
        Queue<string> ____toLoad, Queue<string> ____finalizing,
        ConcurrentDictionary<string, Resource> ____cache)
    {
        // Cache hits are cheap; let a cached session advance without imposing
        // the I/O concurrency limit on every already-loaded path.
        var budget = new AssetPreloadFrameBudget(MaximumRequestCandidatesPerFrame, WorkBudgetMilliseconds);
        while (____loading.Count + ____finalizing.Count < MaximumInFlight && ____toLoad.Count > 0
            && budget.TryBeginItem() && ____toLoad.TryDequeue(out var path))
        {
            if (____cache.ContainsKey(path)) continue;
            var result = ResourceLoader.LoadThreadedRequest(path, "", false, ResourceLoader.CacheMode.Reuse);
            if (result == Error.Ok) ____loading.Enqueue(path);
            else PatchHelper.Log($"[AssetPreload] Error requesting load for {path}: {result}");
        }
        return false;
    }

    private static bool FinalizeLoadingPrefix(Queue<string> ____finalizing,
        ConcurrentDictionary<string, Resource> ____cache, ref int ____totalLoaded)
    {
        var budget = new AssetPreloadFrameBudget(MaximumFinalizationsPerFrame, WorkBudgetMilliseconds);
        while (____finalizing.Count > 0 && budget.TryBeginItem()
            && ____finalizing.TryDequeue(out var path))
        {
            // Only CheckLoadingStatus's Loaded results enter this queue.
            var resource = ResourceLoader.LoadThreadedGet(path);
            if (resource == null)
            {
                PatchHelper.Log($"[AssetPreload] Resource loaded as null: {path}");
                continue;
            }
            ____totalLoaded++;
            ____cache[path] = resource;
        }
        return false;
    }

    private static bool CheckLoadingStatusPrefix(Queue<string> ____loading,
        Queue<string> ____finalizing, ConcurrentDictionary<string, Resource> ____cache,
        AssetCache ____assetCache, ref int ____totalLoaded)
    {
        var count = ____loading.Count;
        if (count == 0) return false;
        var budget = new AssetPreloadFrameBudget(count, WorkBudgetMilliseconds);
        var fallbacks = 0;
        for (var i = 0; i < count && budget.TryBeginItem() && ____loading.TryDequeue(out var path); i++)
        {
            var status = ResourceLoader.LoadThreadedGetStatus(path);
            switch (status)
            {
                case ResourceLoader.ThreadLoadStatus.Loaded:
                    ____finalizing.Enqueue(path);
                    break;
                case ResourceLoader.ThreadLoadStatus.InProgress:
                    ____loading.Enqueue(path);
                    break;
                case ResourceLoader.ThreadLoadStatus.InvalidResource:
                case ResourceLoader.ThreadLoadStatus.Failed:
                    // Preserve the game's fallback and failure accounting, but do
                    // not synchronously drain every failed request in one frame.
                    PatchHelper.Log($"[AssetPreload] Synchronous fallback begin path={path} status={status} thread={System.Environment.CurrentManagedThreadId}");
                    var watch = Stopwatch.StartNew();
                    try
                    {
                        var resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
                        if (resource != null)
                        {
                            ____totalLoaded++;
                            ____cache[path] = resource;
                        }
                        else
                        {
                            ____assetCache?.MarkAssetFailed(path);
                            PatchHelper.Log($"[AssetPreload] Failed to load resource synchronously: {path}");
                        }
                    }
                    finally
                    {
                        PatchHelper.Log($"[AssetPreload] Synchronous fallback end path={path} elapsedMs={watch.Elapsed.TotalMilliseconds:F1}");
                    }
                    if (++fallbacks >= MaximumSynchronousFallbacksPerPass) return false;
                    break;
                default:
                    PatchHelper.Log($"[AssetPreload] Unexpected threaded load status for {path}: {status}");
                    break;
            }
        }
        return false;
    }

    private static void RequireField(Type type, string name, Type expected)
    {
        if (type.GetField(name, TargetMemberFlags)?.FieldType != expected)
            throw new InvalidOperationException($"Asset preload budget unsupported: {type.Name}.{name} changed.");
    }

    private static MethodInfo RequireMethod(Type type, string name)
    {
        var method = type.GetMethod(name, TargetMemberFlags,
            binder: null, types: Type.EmptyTypes, modifiers: null);
        if (method?.ReturnType != typeof(void))
            throw new InvalidOperationException($"Asset preload budget unsupported: {type.Name}.{name} changed.");
        return method;
    }
}
