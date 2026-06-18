using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using STS2Mobile.Patches;

namespace STS2Mobile;

internal static class StartupPatchOrchestrator
{
    private const string ForceCriticalPatchFailureVariable = "STS2_FORCE_CRITICAL_PATCH_FAILURE";

    private static readonly PatchGroup[] Groups = new PatchGroup[]
    {
        Core(),
        Gameplay(),
        Optional()
    };

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

    private static bool ForceCriticalPatchFailureRequested()
        => string.Equals(
            Environment.GetEnvironmentVariable(ForceCriticalPatchFailureVariable),
            "1",
            StringComparison.Ordinal
        );

    private static StartupPatchResult ForcedCriticalPatchFailureResult(Stopwatch stopwatch)
    {
        stopwatch.Stop();
        PatchHelper.Log("[startup] Forced critical patch failure requested for fallback validation");
        return new StartupPatchResult(
            new[]
            {
                new PatchGroupResult(
                    "core",
                    true,
                    stopwatch.Elapsed,
                    new[]
                    {
                        new PatchAttempt(
                            "Forced critical patch failure",
                            false,
                            TimeSpan.Zero,
                            "Injected by STS2_FORCE_CRITICAL_PATCH_FAILURE"
                        )
                    }
                )
            },
            criticalFailed: true,
            stopwatch.Elapsed
        );
    }

    private static PatchGroup Core()
        => new(
            "core",
            true,
            new PatchStep[]
            {
                new("Platform compatibility", PlatformPatches.Apply),
                new("Model DB bootstrap", ModelDbInitPatch.Apply),
                new("Launcher startup gate", LauncherPatches.Apply),
            }
        );

    private static PatchGroup Gameplay()
        => new(
            "gameplay",
            false,
            new PatchStep[]
            {
                new("Settings compatibility", SettingsPatches.Apply),
                new("Font substitution", FontSubstitutionPatches.Apply),
                new("UI scaling", UiScalePatches.Apply),
                new("Mobile layout", MobileLayoutPatches.Apply),
                new("Run history asset fallback", RunHistoryAssetPatches.Apply),
                new("Dev console Android fallback", DevConsolePatches.Apply),
                new("Event layout", EventLayoutPatches.Apply),
                new("Merchant layout", MerchantLayoutPatches.Apply),
                new("App lifecycle", AppLifecyclePatches.Apply),
                new("Touch input", TouchInputPatches.Apply),
                new("Card reward", CardRewardPatches.Apply),
                new("Early access disclaimer", EarlyAccessDisclaimerPatches.Apply),
                new("Combat background", CombatBackgroundPatches.Apply),
            }
        );

    private static PatchGroup Optional()
        => new(
            "optional",
            false,
            new PatchStep[]
            {
                new("LAN multiplayer", LanMultiplayerPatcher.Apply),
                new("Mod loader integration", ModLoaderPatches.Apply),
                new("Save diagnostics", SaveDiagnosticPatches.Apply),
            }
        );

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

    private readonly record struct PatchStep(string Name, Action<Harmony> Apply);

    private readonly record struct PatchGroup(
        string Name,
        bool Critical,
        IReadOnlyList<PatchStep> Steps
    );

    internal sealed record PatchAttempt(
        string Name,
        bool Success,
        TimeSpan Elapsed,
        string? Failure
    );

    internal sealed record PatchGroupResult(
        string Name,
        bool Critical,
        TimeSpan Elapsed,
        IReadOnlyList<PatchAttempt> Attempts
    )
    {
        internal int Applied => Attempts.Count(x => x.Success);
        internal int Total => Attempts.Count;
        internal int Failed => Total - Applied;

        internal IReadOnlyList<string> FailureMessages => Attempts
            .Where(x => !x.Success && !string.IsNullOrWhiteSpace(x.Failure))
            .Select(x => $"{x.Name}: {x.Failure}")
            .ToArray();
    }

    internal sealed class StartupPatchResult
    {
        private readonly IReadOnlyList<PatchGroupResult> _groupResults;

        internal StartupPatchResult(
            IReadOnlyList<PatchGroupResult> groupResults,
            bool criticalFailed,
            TimeSpan duration
        )
        {
            _groupResults = groupResults;
            CriticalFailed = criticalFailed;
            Duration = duration;
        }

        internal bool CriticalFailed { get; }

        internal TimeSpan Duration { get; }

        internal int TotalPatchCount => _groupResults.Sum(x => x.Total);
        internal int AppliedPatchCount => _groupResults.Sum(x => x.Applied);
        internal int FailedPatchCount => _groupResults.Sum(x => x.Failed);

        internal IEnumerable<string> FailureMessages()
        {
            foreach (var group in _groupResults)
            {
                foreach (var failure in group.FailureMessages)
                {
                    yield return $"[{group.Name}] {failure}";
                }
            }
        }

        internal bool HasFailures => FailedPatchCount > 0;
    }
}
