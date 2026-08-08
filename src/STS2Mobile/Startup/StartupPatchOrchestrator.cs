using System;
using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using STS2Mobile.Patches;

namespace STS2Mobile;

internal static partial class StartupPatchOrchestrator
{
    internal static StartupPatchResult Apply(Harmony harmony)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<PatchGroupResult>(Groups.Length);
        var criticalFailed = false;

        if (ForceCriticalPatchFailureRequested())
        {
            var forcedResult = ForcedCriticalPatchFailureResult(stopwatch);
            PatchHelper.Log(
                $"[startup] Patch orchestration finished in {forcedResult.Duration.TotalMilliseconds:F1}ms: "
                + $"{forcedResult.AppliedPatchCount}/{forcedResult.TotalPatchCount} applied, "
                + $"{forcedResult.FailedPatchCount} failed, criticalFailed={forcedResult.CriticalFailed}"
            );
            return forcedResult;
        }

        foreach (var group in Groups)
        {
            BootstrapTrace.Log($"Starting patch group: {group.Name}");
            var groupResult = ApplyGroup(group, harmony);
            BootstrapTrace.Log(
                $"Finished patch group: {group.Name} applied={groupResult.Applied}/{groupResult.Total} failed={groupResult.Failed}"
            );
            results.Add(groupResult);

            if (group.Critical && groupResult.Failed > 0)
            {
                criticalFailed = true;
                break;
            }
        }

        stopwatch.Stop();
        var result = new StartupPatchResult(results, criticalFailed, stopwatch.Elapsed);
        PatchHelper.Log(
            $"[startup] Patch orchestration finished in {result.Duration.TotalMilliseconds:F1}ms: "
            + $"{result.AppliedPatchCount}/{result.TotalPatchCount} applied, "
            + $"{result.FailedPatchCount} failed, criticalFailed={result.CriticalFailed}"
        );

        return result;
    }
}
