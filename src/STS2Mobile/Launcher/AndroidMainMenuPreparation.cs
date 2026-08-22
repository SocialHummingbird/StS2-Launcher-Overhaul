using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class AndroidMainMenuPreparation
{
    private const int ResourceBatchSize = 2;
    private const int ResourceTimeBudgetMs = 3_000;
    private const int RequiredStableFrames = 8;
    private const int StableFrameThresholdMs = 50;
    private const int MinimumFrameObservationMs = 1_500;
    private const int MaximumFrameObservationMs = 5_000;
    private const int OverallPreparationDeadlineMs = 8_500;

    private sealed class PreparationState
    {
        internal readonly List<Resource> RetainedResources = new();
        internal readonly List<Material> RenderMaterials = new();
        internal readonly List<AndroidMainMenuResourceResolution> Resolutions = new();
        internal readonly List<string> MissingLogicalPaths = new();
        internal AndroidMainMenuWorkingSetIdentity Identity =
            AndroidMainMenuWorkingSetIdentity.Missing();
        internal int Attempted;
        internal int Loaded;
        internal int Missing;
        internal int Failed;
        internal int DuplicateResources;
        internal int RejectedResources;
        internal int RenderedMaterials;
        internal bool ResourceBudgetReached;
        internal long ResourceElapsedMs;
    }

    internal static async Task<AndroidMainMenuPreparationResult> RunAsync(
        Node gameNode,
        Label startupStatus
    )
    {
        if (!OperatingSystem.IsAndroid())
            return AndroidMainMenuPreparationResult.NotRequired();

        var total = Stopwatch.StartNew();
        var deadline = LauncherMonotonicDeadline.Start(
            TimeSpan.FromMilliseconds(OverallPreparationDeadlineMs)
        );
        var lifecycle = new LauncherOperationLifecycle();
        var lifecycleMonitor = new LauncherOperationLifecycleMonitor(
            lifecycle,
            deadline
        );
        var state = new PreparationState();
        var stability = new MainMenuFrameStabilityTracker(
            RequiredStableFrames,
            StableFrameThresholdMs,
            MinimumFrameObservationMs,
            MaximumFrameObservationMs
        );
        var result = AndroidMainMenuPreparationResult.Failed(
            "main-menu preparation did not complete"
        );
        long stabilityElapsedMs = 0;

        try
        {
            gameNode.AddChild(lifecycleMonitor);
            if (OperatingSystem.IsAndroid())
            {
                try
                {
                    var visibility = LauncherHandoffVisibility.Capture();
                    lifecycleMonitor.ObserveApplicationActive(
                        !visibility.SuspendsHandoffTimeout
                    );
                }
                catch (Exception ex)
                {
                    PatchHelper.Log(
                        $"[MainMenuPreparation] Android lifecycle state unavailable: {ex.Message}"
                    );
                }
            }
            state.Identity = ObserveCurrentMountedResourceSetIdentity();
            if (!IsPreparationTargetAlive(gameNode))
            {
                stability.TryAbort();
                result = AndroidMainMenuPreparationResult.Aborted();
                return result;
            }

            LauncherStartupStatus.Set(
                startupStatus,
                "Preparing home screen resources..."
            );
            await PreloadWorkingSetAsync(
                gameNode,
                startupStatus,
                state,
                deadline,
                lifecycle
            );

            if (!IsPreparationTargetAlive(gameNode))
            {
                stability.TryAbort();
                result = AndroidMainMenuPreparationResult.Aborted();
                return result;
            }

            LauncherStartupStatus.Set(
                startupStatus,
                state.ResourceBudgetReached
                    ? "Resource preload budget reached. Checking rendered frames..."
                    : "Waiting for stable rendered frames..."
            );
            (result, stabilityElapsedMs) = await WaitForStableRenderedFramesAsync(
                stability,
                gameNode,
                deadline,
                lifecycle
            );
            if (!IsPreparationTargetAlive(gameNode))
            {
                stability.TryAbort();
                result = AndroidMainMenuPreparationResult.Aborted();
                return result;
            }

            LauncherStartupStatus.Set(
                startupStatus,
                result.CanExposeMainMenu
                    ? "Home screen rendering is stable."
                    : "Home screen rendering did not stabilize. Recovery options are available."
            );
        }
        catch (Exception ex)
        {
            state.Failed++;
            result = AndroidMainMenuPreparationResult.Failed(
                $"{ex.GetBaseException().GetType().Name}: {ex.GetBaseException().Message}"
            );
            PatchHelper.Log($"[MainMenuPreparation] Preparation failed: {ex}");
        }
        finally
        {
            lifecycle.Destroy();
            if (GodotObject.IsInstanceValid(lifecycleMonitor))
                lifecycleMonitor.QueueFree();
            total.Stop();
            WriteEvidence(
                state,
                stability,
                result,
                stabilityElapsedMs,
                total.ElapsedMilliseconds
            );
            state.RenderMaterials.Clear();
            state.RetainedResources.Clear();
            state.Resolutions.Clear();
            state.MissingLogicalPaths.Clear();
        }

        return result;
    }

    private static bool IsPreparationTargetAlive(Node gameNode)
    {
        try
        {
            return GodotObject.IsInstanceValid(gameNode)
                && gameNode.IsInsideTree()
                && !gameNode.IsQueuedForDeletion();
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private static AndroidMainMenuWorkingSetIdentity CaptureWorkingSetIdentity()
    {
        try
        {
            var attempt = LauncherLaunchMarkers.ReadLastLaunchAttempt();
            return attempt.Present
                ? AndroidMainMenuWorkingSetIdentity.Create(
                    attempt.SelectedBranch,
                    attempt.PckSha256,
                    attempt.ModPlayMode
                )
                : AndroidMainMenuWorkingSetIdentity.Missing();
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[MainMenuPreparation] Runtime identity unavailable: {ex.Message}"
            );
            return AndroidMainMenuWorkingSetIdentity.Missing();
        }
    }

    internal static AndroidMainMenuWorkingSetIdentity ObserveCurrentMountedResourceSetIdentity()
    {
        var identity = CaptureWorkingSetIdentity();
        AndroidAtlasCompatibilityPatches.ObserveMountedResourceSetIdentity(
            identity.ResourceSetKey
        );
        return identity;
    }
}
