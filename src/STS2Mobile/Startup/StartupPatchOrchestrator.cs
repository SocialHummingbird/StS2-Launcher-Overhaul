using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using STS2Mobile.Patches;

namespace STS2Mobile;

internal static class StartupPatchOrchestrator
{
    private readonly record struct PatchStep(string Name, Action<Harmony> Apply);
    private readonly record struct PatchGroup(string Name, bool Critical, IReadOnlyList<PatchStep> Steps);

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
        public int Applied => Attempts.Count(x => x.Success);
        public int Total => Attempts.Count;
        public int Failed => Total - Applied;

        public IReadOnlyList<string> FailureMessages => Attempts
            .Where(x => !x.Success && !string.IsNullOrWhiteSpace(x.Failure))
            .Select(x => $"{x.Name}: {x.Failure}")
            .ToArray();
    }

    private static readonly PatchGroup[] Groups = new PatchGroup[]
    {
            new PatchGroup(
                "core",
                true,
                new PatchStep[]
                {
                    new PatchStep("Platform compatibility", PlatformPatches.Apply),
                    new PatchStep("Model DB bootstrap", ModelDbInitPatch.Apply),
                }
            ),
        new PatchGroup(
            "gameplay",
            false,
                new PatchStep[]
                {
                    new PatchStep("Settings compatibility", SettingsPatches.Apply),
                    new PatchStep("Font substitution", FontSubstitutionPatches.Apply),
                    new PatchStep("UI scaling", UiScalePatches.Apply),
                    new PatchStep("Mobile layout", MobileLayoutPatches.Apply),
                new PatchStep("Event layout", EventLayoutPatches.Apply),
                new PatchStep("Merchant layout", MerchantLayoutPatches.Apply),
                new PatchStep("App lifecycle", AppLifecyclePatches.Apply),
                new PatchStep("Touch input", TouchInputPatches.Apply),
                new PatchStep("Card reward", CardRewardPatches.Apply),
                new PatchStep("Early access disclaimer", EarlyAccessDisclaimerPatches.Apply),
                new PatchStep("Combat background", CombatBackgroundPatches.Apply),
            }
        ),
        new PatchGroup(
            "optional",
            false,
            new PatchStep[]
            {
                new PatchStep("LAN multiplayer", LanMultiplayerPatcher.Apply),
                new PatchStep("Mod loader integration", ModLoaderPatches.Apply),
                new PatchStep("Launcher UI", LauncherPatches.Apply),
                new PatchStep("Save diagnostics", SaveDiagnosticPatches.Apply),
            }
        )
    };

    internal static StartupPatchResult Apply(Harmony harmony)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<PatchGroupResult>(Groups.Length);
        var criticalFailed = false;

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

    private static PatchGroupResult ApplyGroup(PatchGroup group, Harmony harmony)
    {
        var groupStopwatch = Stopwatch.StartNew();
        var attempts = new List<PatchAttempt>();

        for (var i = 0; i < group.Steps.Count; i++)
        {
            var step = group.Steps[i];
            var patchStopwatch = Stopwatch.StartNew();
            try
            {
                BootstrapTrace.Log($"Starting patch step: {group.Name}/{step.Name}");
                step.Apply(harmony);
                BootstrapTrace.Log($"Finished patch step: {group.Name}/{step.Name}");
                attempts.Add(new PatchAttempt(step.Name, true, patchStopwatch.Elapsed, null));
            }
            catch (Exception ex)
            {
                var failure = $"{ex.GetType().Name}: {ex.Message}";
                BootstrapTrace.Log($"Failed patch step: {group.Name}/{step.Name}: {failure}");
                attempts.Add(new PatchAttempt(step.Name, false, patchStopwatch.Elapsed, failure));
                PatchHelper.Log($"[{group.Name}] {step.Name} failed: {failure}");
            }
            finally
            {
                patchStopwatch.Stop();
            }
        }

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

    public sealed record StartupPatchResult(
        IReadOnlyList<PatchGroupResult> GroupResults,
        bool CriticalFailed,
        TimeSpan Duration
    )
    {
        public int TotalPatchCount => GroupResults.Sum(x => x.Total);
        public int AppliedPatchCount => GroupResults.Sum(x => x.Applied);
        public int FailedPatchCount => GroupResults.Sum(x => x.Failed);

        public IEnumerable<string> FailureMessages()
        {
            foreach (var group in GroupResults)
            {
                foreach (var failure in group.FailureMessages)
                {
                    yield return $"[{group.Name}] {failure}";
                }
            }
        }

        public bool HasFailures => FailedPatchCount > 0;
    };
}
