using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class AndroidMainMenuPreparation
{
    private static void WriteEvidence(
        PreparationState state,
        MainMenuFrameStabilityTracker stability,
        AndroidMainMenuPreparationResult result,
        long stabilityElapsedMs,
        long totalElapsedMs
    )
    {
        var classification = result.Outcome switch
        {
            AndroidMainMenuPreparationOutcome.StableRenderedFrames
                => "stable rendered-frame handoff",
            AndroidMainMenuPreparationOutcome.TimedOut
                => "rendered-frame timeout blocked handoff",
            AndroidMainMenuPreparationOutcome.Aborted
                => "lifecycle teardown blocked handoff",
            AndroidMainMenuPreparationOutcome.Failed
                => "rendered-frame preparation failed",
            _ => "rendered-frame preparation not required",
        };
        var writeFullDiagnostics =
            PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(
                PostStartupDiagnosticsSettings.DetailedTraceEnabled(),
                failureOrRecovery: !result.CanExposeMainMenu
        );
        if (writeFullDiagnostics)
        {
            WriteDetailedEvidence(
                classification,
                state,
                stability,
                result,
                stabilityElapsedMs,
                totalElapsedMs
            );
        }
        else
        {
            WriteLightweightHeartbeat(
                classification,
                state,
                stability,
                result,
                totalElapsedMs
            );
        }

        PatchHelper.Log(
            $"[MainMenuPreparation] {classification}; fullDiagnostics={writeFullDiagnostics} "
            + $"handoff={result.CanExposeMainMenu} resources={state.Loaded}/{state.Attempted} "
            + $"missing={state.Missing} failed={state.Failed} rendered={state.RenderedMaterials} "
            + $"resourceBudgetReached={state.ResourceBudgetReached} "
            + $"framePostDraw={stability.FramePostDrawSamples} slow={stability.SlowFrames} "
            + $"consecutive={stability.ConsecutiveStableFrames} maxFrameMs={stability.MaximumFrameIntervalMs} "
            + $"resourceMs={state.ResourceElapsedMs} stabilityMs={stabilityElapsedMs} totalMs={totalElapsedMs}"
        );
    }

    private static void WriteDetailedEvidence(
        string classification,
        PreparationState state,
        MainMenuFrameStabilityTracker stability,
        AndroidMainMenuPreparationResult result,
        long stabilityElapsedMs,
        long totalElapsedMs
    )
    {
        var details = new System.Collections.Generic.List<string>
        {
            $"Handoff allowed: {result.CanExposeMainMenu}",
            $"Handoff detail: {result.Detail}",
            $"Selected branch: {state.Identity.Branch}",
            $"Selected PCK identity: {state.Identity.PckIdentity}",
            $"Mod play mode: {state.Identity.ModMode}",
            "Resource resolution: active Godot virtual filesystem after mod-pack mounting",
            $"Overall deadline ms: {OverallPreparationDeadlineMs}",
            $"Working set configured: {AndroidMainMenuWorkingSet.Resources.Length}/{AndroidMainMenuWorkingSet.MaximumResourceCount}",
            $"Resources attempted: {state.Attempted}",
            $"Resources loaded: {state.Loaded}",
            $"Resources missing: {state.Missing}",
            $"Resources failed: {state.Failed}",
            $"Duplicate logical resources skipped: {state.DuplicateResources}",
            $"Rejected non-logical resources: {state.RejectedResources}",
            "Working-set preload is advisory: True",
            $"Render materials exercised: {state.RenderedMaterials}/{state.RenderMaterials.Count}",
            $"Resource budget reached: {state.ResourceBudgetReached}",
            $"Resource preparation elapsed ms: {state.ResourceElapsedMs}",
            $"FramePostDraw stability outcome: {stability.Outcome}",
            $"FramePostDraw stability elapsed ms: {stabilityElapsedMs}",
            $"FramePostDraw samples: {stability.FramePostDrawSamples}",
            $"Slow rendered frames: {stability.SlowFrames}",
            $"Latest rendered-frame interval ms: {stability.LatestFrameIntervalMs}",
            $"Average rendered-frame interval ms: {stability.AverageFrameIntervalMs}",
            $"Maximum rendered-frame interval ms: {stability.MaximumFrameIntervalMs}",
            $"Consecutive stable rendered frames: {stability.ConsecutiveStableFrames}",
            $"Total preparation elapsed ms: {totalElapsedMs}",
        };
        AddResourceResolutionEvidence(state, details);
        LauncherDiagnostics.WriteMainMenuPreparation(
            classification,
            details.ToArray()
        );
    }

    private static void WriteLightweightHeartbeat(
        string classification,
        PreparationState state,
        MainMenuFrameStabilityTracker stability,
        AndroidMainMenuPreparationResult result,
        long totalElapsedMs
    )
    {
        LauncherDiagnostics.WriteMainMenuPreparation(
            classification,
            "Evidence level: lightweight heartbeat",
            $"Handoff allowed: {result.CanExposeMainMenu}",
            $"Selected branch: {state.Identity.Branch}",
            $"Selected PCK identity: {state.Identity.PckIdentity}",
            $"Mod play mode: {state.Identity.ModMode}",
            $"Resources loaded: {state.Loaded}/{state.Attempted}",
            $"Resources missing: {state.Missing}",
            $"Resource budget reached: {state.ResourceBudgetReached}",
            $"FramePostDraw samples: {stability.FramePostDrawSamples}",
            $"Slow rendered frames: {stability.SlowFrames}",
            $"Maximum rendered-frame interval ms: {stability.MaximumFrameIntervalMs}",
            $"Consecutive stable rendered frames: {stability.ConsecutiveStableFrames}",
            $"Total preparation elapsed ms: {totalElapsedMs}"
        );
    }

    private static void AddResourceResolutionEvidence(
        PreparationState state,
        System.Collections.Generic.List<string> details
    )
    {
        for (var index = 0; index < state.Resolutions.Count; index++)
        {
            var resolution = state.Resolutions[index];
            details.Add(
                $"Resolved resource {index + 1}: logical={resolution.LogicalPath}; effective={resolution.EffectivePath}"
            );
        }
        for (var index = 0; index < state.MissingLogicalPaths.Count; index++)
        {
            details.Add(
                $"Missing logical resource {index + 1}: {state.MissingLogicalPaths[index]}"
            );
        }
    }
}
