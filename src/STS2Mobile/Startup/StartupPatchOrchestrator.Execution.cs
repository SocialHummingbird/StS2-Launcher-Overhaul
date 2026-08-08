using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using STS2Mobile.Patches;

namespace STS2Mobile;

internal static partial class StartupPatchOrchestrator
{
    private static PatchGroupResult ApplyGroup(PatchGroup group, Harmony harmony)
    {
        var groupStopwatch = Stopwatch.StartNew();
        var attempts = new List<PatchAttempt>();

        for (var i = 0; i < group.Steps.Count; i++)
            attempts.Add(ApplyStep(group, group.Steps[i], harmony));

        groupStopwatch.Stop();
        var failed = attempts.Count(x => !x.Success);
        var result = new PatchGroupResult(
            group.Name,
            group.Critical,
            groupStopwatch.Elapsed,
            attempts
        );
        PatchHelper.Log(
            $"[{group.Name}] {result.Applied}/{result.Total} patches applied, {failed} failed in {result.Elapsed.TotalMilliseconds:F1}ms"
        );
        return result;
    }

    private static PatchAttempt ApplyStep(PatchGroup group, PatchStep step, Harmony harmony)
    {
        var patchStopwatch = Stopwatch.StartNew();
        var helperFailures = new List<string>();
        void CaptureHelperFailure(string message)
        {
            if (IsHelperPatchFailure(message))
                helperFailures.Add(message);
        }

        PatchHelper.LogEmitted += CaptureHelperFailure;
        try
        {
            BootstrapTrace.Log($"Starting patch step: {group.Name}/{step.Name}");
            step.Apply(harmony);
            if (helperFailures.Count > 0)
            {
                var failure = FormatHelperFailures(helperFailures);
                BootstrapTrace.Log($"Failed patch step: {group.Name}/{step.Name}: {failure}");
                PatchHelper.Log($"[{group.Name}] {step.Name} failed: {failure}");
                return new PatchAttempt(step.Name, false, patchStopwatch.Elapsed, failure);
            }

            BootstrapTrace.Log($"Finished patch step: {group.Name}/{step.Name}");
            return new PatchAttempt(step.Name, true, patchStopwatch.Elapsed, null);
        }
        catch (Exception ex)
        {
            var failure = $"{ex.GetType().Name}: {ex.Message}";
            BootstrapTrace.Log($"Failed patch step: {group.Name}/{step.Name}: {failure}");
            PatchHelper.Log($"[{group.Name}] {step.Name} failed: {failure}");
            return new PatchAttempt(step.Name, false, patchStopwatch.Elapsed, failure);
        }
        finally
        {
            PatchHelper.LogEmitted -= CaptureHelperFailure;
            patchStopwatch.Stop();
        }
    }

    private static bool IsHelperPatchFailure(string message)
        => message.StartsWith("FAILED ", StringComparison.Ordinal)
            || message.Contains(" patch failed:", StringComparison.OrdinalIgnoreCase);

    private static string FormatHelperFailures(IReadOnlyList<string> failures)
    {
        const int maxFailures = 3;
        var selected = failures.Take(maxFailures).ToArray();
        var suffix = failures.Count > maxFailures
            ? $" (+{failures.Count - maxFailures} more)"
            : "";
        return string.Join("; ", selected) + suffix;
    }
}
