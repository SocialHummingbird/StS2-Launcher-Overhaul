using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using HarmonyLib;
using STS2Mobile.Launcher;

namespace STS2Mobile;

// Entry point for the mobile patcher. Bootstraps GodotSharp, applies all Harmony
// patches, and falls back to standalone launcher mode if game files aren't present.
public static class ModEntry
{
    private const string HarmonyId = "com.sts2mobile";
    private const int NotStarted = 0;
    private const int InProgress = 1;
    private const int Complete = 2;

    private static int _applyState = NotStarted;

    // Bootstraps GodotSharp by setting up DLL import resolver, native interop,
    // and managed callbacks. Called from gd_mono.cpp before Apply().
    [UnmanagedCallersOnly]
    public static int InitializeGodotSharp(
        IntPtr godotDllHandle,
        IntPtr outManagedCallbacks,
        IntPtr unmanagedCallbacks,
        int unmanagedCallbacksSize
    )
    {
        try
        {
            DllImportResolver dllImportResolver = new GodotDllImportResolver(godotDllHandle).OnResolveDllImport;
            var coreApiAssembly = typeof(GodotObject).Assembly;
            NativeLibrary.SetDllImportResolver(coreApiAssembly, dllImportResolver);

            NativeFuncs.Initialize(unmanagedCallbacks, unmanagedCallbacksSize);
            ManagedCallbacks.Create(outManagedCallbacks);

            Console.Error.WriteLine("[STS2Mobile] GodotSharp bootstrapped successfully");
            return 1;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"[STS2Mobile] GodotSharp bootstrap failed: {e}");
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    public static void Apply()
    {
        if (Interlocked.CompareExchange(ref _applyState, InProgress, NotStarted) != NotStarted)
        {
            PatchHelper.Log("Apply already running/completed; skipping duplicate invocation.");
            return;
        }

        try
        {
            ApplyStartupPatches();
        }
        finally
        {
            _applyState = Complete;
        }
    }

    private static void ApplyStartupPatches()
    {
        PatchHelper.Log("Initializing STS2Mobile...");
        try
        {
            var harmony = new Harmony(HarmonyId);
            var patchResult = StartupPatchOrchestrator.Apply(harmony);

            if (patchResult.CriticalFailed)
            {
                PatchHelper.Log("Critical startup patches failed; scheduling standalone launcher fallback.");
                ScheduleStandaloneLauncher();
                return;
            }

            if (patchResult.HasFailures)
            {
                PatchHelper.Log(
                    $"Startup completed with {patchResult.FailedPatchCount} non-critical patch failures."
                );
            }

            foreach (var failure in patchResult.FailureMessages().Take(10))
            {
                PatchHelper.Log($"[startup] {failure}");
            }

            PatchHelper.Log("Startup patch orchestration complete.");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Unexpected startup error: {ex.Message}");
            ScheduleStandaloneLauncher();
        }
    }

    private static void ScheduleStandaloneLauncher()
    {
        PatchHelper.Log("Scheduling standalone launcher...");
        Callable.From(CreateStandaloneLauncher).CallDeferred();
    }

    private static void CreateStandaloneLauncher()
    {
        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            Callable.From(CreateStandaloneLauncher).CallDeferred();
            return;
        }

        var launcher = new LauncherUI();
        tree.Root.AddChild(launcher);
        launcher.Initialize();
        PatchHelper.Log("Standalone launcher displayed");
    }
}
