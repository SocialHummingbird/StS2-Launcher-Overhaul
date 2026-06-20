using System;
using System.Linq;
using System.Threading;
using Godot;
using HarmonyLib;
using STS2Mobile.Launcher;

namespace STS2Mobile;

public static partial class ModEntry
{
    private static void ApplyInternal()
    {
        BootstrapTrace.Log("ApplyInternal entered");
        InstallManagedExceptionHandlers();
        if (!TryBeginApply())
        {
            BootstrapTrace.Log("ApplyInternal duplicate invocation skipped");
            PatchHelper.Log("Apply already running/completed; skipping duplicate invocation.");
            return;
        }

        try
        {
            ApplyStartupPatches();
        }
        finally
        {
            CompleteApply();
        }
    }

    private static void ApplyStartupPatches()
    {
        BootstrapTrace.Log("Initializing STS2Mobile");
        PatchHelper.Log("Initializing STS2Mobile...");
        try
        {
            ConfigureWritableTempDirectory();
            var launcherBootstrapRequested = IsLauncherBootstrapRequested();
            if (launcherBootstrapRequested)
            {
                PatchHelper.Log("Launcher bootstrap mode requested; skipping game startup patch orchestration.");
                ScheduleStandaloneLauncher();
                return;
            }

            var harmony = new Harmony(HarmonyId);
            BootstrapTrace.Log("Starting startup patch orchestration");
            var patchResult = StartupPatchOrchestrator.Apply(harmony);
            BootstrapTrace.Log("Finished startup patch orchestration");
            LauncherRuntimePatchValidationEvidence.Write(OS.GetDataDir(), patchResult);

            if (patchResult.CriticalFailed)
            {
                var reason = "Critical startup patches failed:\n"
                    + string.Join("\n", patchResult.FailureMessages().Take(5));
                PatchHelper.Log("Critical startup patches failed; scheduling standalone launcher fallback.");
                ScheduleStandaloneLauncher(reason);
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
            if (IsStandaloneLauncherRequired())
            {
                ScheduleStandaloneLauncher("Game files are not ready. Launcher-only mode started.");
            }
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"Unexpected startup error: {ex.Message}");
            ScheduleStandaloneLauncher($"Unexpected startup error:\n{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool IsLauncherBootstrapRequested()
        => string.Equals(
            System.Environment.GetEnvironmentVariable(LauncherBootstrapVariable),
            "1",
            StringComparison.Ordinal
        );

    private static bool TryBeginApply()
    {
        return Interlocked.CompareExchange(ref _applyState, ApplyInProgress, ApplyNotStarted) == ApplyNotStarted;
    }

    private static void CompleteApply()
    {
        _applyState = ApplyComplete;
    }
}
