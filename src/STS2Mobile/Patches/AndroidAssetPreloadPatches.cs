using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    private const int MaximumRequestCandidatesPerFrame = 64;
    private const double WorkBudgetMilliseconds = 4;

    // Android runs a copy of sts2.dll rewritten by AndroidAssemblyPublicizer, so
    // these members are private on desktop but public on device. Match either.
    private const BindingFlags TargetMemberFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static void Apply(Harmony harmony)
    {
        if (OperatingSystem.IsAndroid()) ApplyForRuntime(harmony);
    }

    // Also exercised with real threaded resources by the desktop Godot probe.
    internal static void ApplyForRuntime(Harmony harmony)
    {
        var (request, finalize) = RequireSupportedSession(typeof(AssetLoadingSession));
        harmony.Patch(request, prefix: new HarmonyMethod(
            PatchHelper.Method(typeof(AndroidAssetPreloadPatches), nameof(ProcessLoadingQueuePrefix))));
        harmony.Patch(finalize, prefix: new HarmonyMethod(
            PatchHelper.Method(typeof(AndroidAssetPreloadPatches), nameof(FinalizeLoadingPrefix))));
        PatchHelper.Log($"[AssetPreload] Frame budgets installed: inFlight={MaximumInFlight} "
            + $"finalize={MaximumFinalizationsPerFrame} budgetMs={WorkBudgetMilliseconds}");
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

    internal static (MethodInfo Request, MethodInfo Finalize) RequireSupportedSession(Type type)
    {
        RequireField(type, "_loading", typeof(Queue<string>));
        RequireField(type, "_toLoad", typeof(Queue<string>));
        RequireField(type, "_finalizing", typeof(Queue<string>));
        RequireField(type, "_cache", typeof(ConcurrentDictionary<string, Resource>));
        RequireField(type, "_totalLoaded", typeof(int));
        return (RequireMethod(type, "ProcessLoadingQueue"), RequireMethod(type, "FinalizeLoading"));
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
