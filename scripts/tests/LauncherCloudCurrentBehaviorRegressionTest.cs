using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

// Coordinator-level assertions verify repaired cloud workflow contracts.
internal sealed partial class LauncherCloudSyncCoordinator
{
    internal Task ExecuteSyntheticRequestAsync(
        string name,
        Func<CancellationToken, Task<string>> run,
        int timeoutMs = 2_000
    )
        => StartCloudSync(
            ManualCloudSyncRequest.Synthetic(name, run, timeoutMs)
        );

    internal Task ExecuteSyntheticPullRequestAsync(
        Func<
            CloudOperationProgressTracker,
            CancellationToken,
            Task<string>
        > run,
        int timeoutMs = 2_000
    )
    {
        var progress = new CloudOperationProgressTracker(
            CloudOperationKind.Pull,
            ReportCloudOperationState
        );
        progress.Preparing("Checking saved Steam login");
        return StartCloudSync(
            ManualCloudSyncRequest.Synthetic(
                "Pull",
                cancellationToken => run(progress, cancellationToken),
                timeoutMs,
                progress
            )
        );
    }

    internal void RequestSyntheticSafetyCheckedPush(
        string expectedBranch,
        Action onRun
    )
        => RequestCloudSync(
            ManualCloudSyncRequest.SafetyCheckedPush(
                _model.DataDir,
                expectedBranch,
                onRun
            )
        );

    internal void RequestSyntheticSync(
        string name,
        Func<CancellationToken, Task<string>> run,
        int timeoutMs = 2_000
    )
        => RequestCloudSync(
            ManualCloudSyncRequest.Synthetic(name, run, timeoutMs)
        );

    private readonly partial struct ManualCloudSyncRequest
    {
        internal static ManualCloudSyncRequest Synthetic(
            string name,
            Func<CancellationToken, Task<string>> run,
            int timeoutMs,
            CloudOperationProgressTracker? progress = null
        )
            => new(
                $"{name} confirmation",
                name,
                "Cancel",
                name,
                $"{name} operation started.",
                true,
                async cancellationToken =>
                {
                    var detail = await run(cancellationToken);
                    progress?.Completed(detail);
                    var kind = string.Equals(
                        name,
                        "Pull",
                        StringComparison.OrdinalIgnoreCase
                    )
                        ? CloudOperationKind.Pull
                        : CloudOperationKind.Push;
                    return new ManualCloudSyncResult(
                        kind,
                        1,
                        1,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        detail
                    );
                },
                timeoutMs: timeoutMs,
                operationProgress: progress
            );

        internal static ManualCloudSyncRequest Pull(
            string dataDir,
            string selectedBranch,
            CloudOperationProgressTracker progress
        )
            => Synthetic(
                "Pull",
                _ => Task.FromResult("Synthetic Pull"),
                CloudSyncTimeoutMs,
                progress
            );

        internal static ManualCloudSyncRequest Push(
            string dataDir,
            string selectedBranch,
            CloudOperationProgressTracker progress
        )
            => Synthetic(
                "Push",
                _ => Task.FromResult("Synthetic Push"),
                CloudSyncTimeoutMs,
                progress
            );

        internal static ManualCloudSyncRequest SafetyCheckedPush(
            string dataDir,
            string selectedBranch,
            Action onRun
        )
            => new(
                "Synthetic Push confirmation",
                "Push",
                "Cancel",
                "Push",
                "Synthetic Push operation started.",
                false,
                _ =>
                {
                    onRun();
                    return Task.FromResult(
                        new ManualCloudSyncResult(
                            CloudOperationKind.Push,
                            1,
                            1,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            "Synthetic Push"
                        )
                    );
                },
                prepareOperation: () =>
                    EnsureCloudPushStillEligible(
                        dataDir,
                        selectedBranch
                    ),
                onFailed: ex =>
                    LauncherCloudSyncEvidence.WriteManualPushBlockedMarker(
                        dataDir,
                        selectedBranch,
                        ex.Message
                    )
            );
    }
}

internal static class LauncherCloudCurrentBehaviorRegressionTest
{
    private static int _fixed;

    private static async Task<int> Main()
    {
        await VerifyObservablePullProgressAsync();
        await VerifyFixedAsync(
            "Pull disables both cloud controls while observable work is active",
            VerifyCloudControlsDisabledDuringOperationAsync
        );
        VerifyStructuredUploadEligibility();
        await VerifyFixedAsync(
            "stale Upload approval is blocked before cloud writes",
            VerifyStaleUploadApprovalIsBlockedAsync
        );
        await VerifyFixedAsync(
            "Pull applies refreshed eligibility before restoring controls",
            VerifyPullRefreshesEligibilityBeforeControlsAsync
        );
        await VerifyFixedAsync(
            "backup recovery publishes refreshed save state",
            VerifyBackupRecoveryRefreshesStateAsync
        );
        await VerifyFixedAsync(
            "duplicate taps cannot start concurrent cloud operations",
            VerifyDuplicateCloudRequestsAreRejectedAsync
        );
        await VerifyFixedAsync(
            "timeout cancels and drains cloud work before restoring the UI",
            VerifyTimeoutCancelsAndDrainsAsync
        );
        await VerifyFixedAsync(
            "explicit cancellation drains cloud work before restoring the UI",
            VerifyExplicitCancellationAsync
        );
        await VerifyFixedAsync(
            "lifecycle disposal cancels and drains cloud work",
            VerifyLifecycleDisposalAsync
        );

        AssertEqual("repaired contracts verified", 10, _fixed);
        Console.WriteLine(
            "Launcher cloud current-behaviour checks verified 10 repaired contracts."
        );
        return 0;
    }

    private static async Task VerifyFixedAsync(
        string name,
        Func<Task> verify
    )
    {
        await verify();
        _fixed++;
        Console.WriteLine($"[FIXED] {name}.");
    }

    private static async Task VerifyObservablePullProgressAsync()
    {
        CloudBehaviorGateState.Reset();
        var pending = NewCompletionSource<string>();
        var entered = NewCompletionSource<bool>();
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);

        var execution = coordinator.ExecuteSyntheticPullRequestAsync(
            async (progress, cancellationToken) =>
            {
                progress.EnumerationStarted("Checking Steam Cloud save locations");
                progress.EnumerationCompleted(4);
                progress.BackupStarted(4);
                progress.BackupProcessed(
                    "profile1/saves/progress.save",
                    created: true
                );
                progress.TransferStarted(4);
                entered.TrySetResult(true);
                return await pending.Task.WaitAsync(cancellationToken);
            }
        );

        await entered.Task;
        await Task.Delay(30);

        var pendingState = view.CloudOperationStates.Last();
        AssertEqual(
            "pending Pull phase",
            CloudOperationPhase.Transferring,
            pendingState.Phase
        );
        AssertTrue("pending Pull is active", pendingState.IsActive);
        AssertEqual("pending Pull transfer total", 4, pendingState.TransferTotalCount);
        AssertTrue(
            "pending Pull emitted multiple observable states",
            view.CloudOperationStates.Count >= 6
        );

        pending.SetResult("downloaded");
        await execution;
        var completeState = view.CloudOperationStates.Last();
        AssertEqual(
            "completed Pull phase",
            CloudOperationPhase.Completed,
            completeState.Phase
        );
        AssertTrue("completed Pull is terminal", completeState.IsTerminal);
        _fixed++;
        Console.WriteLine(
            "[FIXED] Pull exposes live cloud-operation phases and counts while work is pending."
        );
    }

    private static async Task VerifyCloudControlsDisabledDuringOperationAsync()
    {
        CloudBehaviorGateState.Reset();
        var pending = NewCompletionSource<string>();
        var entered = NewCompletionSource<bool>();
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);

        var execution = coordinator.ExecuteSyntheticRequestAsync(
            "Pull",
            async cancellationToken =>
            {
                entered.TrySetResult(true);
                return await pending.Task.WaitAsync(cancellationToken);
            }
        );

        await entered.Task;
        AssertTrue(
            "Pull must disable Pull/Push controls while active",
            view.PushPullDisabled
        );
        AssertSequence(
            "control state while Pull is pending",
            new[] { true },
            view.PushPullDisabledChanges.ToArray()
        );

        pending.SetResult("downloaded");
        await execution;
        AssertFalse(
            "cloud controls remain disabled after Pull",
            view.PushPullDisabled
        );
        AssertSequence(
            "control state around opaque Pull",
            new[] { true, false },
            view.PushPullDisabledChanges.ToArray()
        );
    }

    private static void VerifyStructuredUploadEligibility()
    {
        var cases = new (
            string Name,
            Action Configure,
            CloudPushEligibilityBlockCode ExpectedCode
        )[]
        {
            (
                "mods selected",
                () => LauncherWorkshopModSafety.SelectedModCount = 2,
                CloudPushEligibilityBlockCode.ModsSelected
            ),
            (
                "Pull absent",
                () => LauncherCloudSyncEvidence.PullCompletionRecorded = false,
                CloudPushEligibilityBlockCode.ManualPullNotCompleted
            ),
            (
                "Pull belongs to another version",
                () => LauncherCloudSyncEvidence.PullMatchesSelectedBranch = false,
                CloudPushEligibilityBlockCode.ManualPullVersionMismatch
            ),
            (
                "local saves absent",
                () => LauncherLocalSaveEvidence.ImportantSaveEvidenceAvailable = false,
                CloudPushEligibilityBlockCode.ImportantLocalSavesMissing
            ),
            (
                "save origin mismatches runtime",
                () => LauncherSaveOriginEvidence.MatchesSelectedRuntime = false,
                CloudPushEligibilityBlockCode.LocalSaveOriginNotVerified
            ),
            (
                "branch-switch evidence invalid",
                () =>
                {
                    LauncherBranchSwitchSafety.MarkerPresent = true;
                    LauncherBranchSwitchSafety.RequiredEvidenceAvailable = false;
                    LauncherPreferences.LocalBackupEnabled = true;
                },
                CloudPushEligibilityBlockCode.BranchSwitchEvidenceInvalid
            ),
            (
                "Pull-after-switch absent",
                () =>
                {
                    LauncherBranchSwitchSafety.MarkerPresent = true;
                    LauncherCloudSyncEvidence.PullAfterBranchSwitch = false;
                    LauncherPreferences.LocalBackupEnabled = true;
                },
                CloudPushEligibilityBlockCode.ManualPullAfterBranchSwitchMissing
            ),
            (
                "local backup disabled after branch switch",
                () =>
                {
                    LauncherBranchSwitchSafety.MarkerPresent = true;
                    LauncherPreferences.LocalBackupEnabled = false;
                },
                CloudPushEligibilityBlockCode.LocalBackupDisabledAfterBranchSwitch
            ),
            (
                "backup permission absent",
                () =>
                {
                    LauncherBranchSwitchSafety.MarkerPresent = true;
                    LauncherPreferences.LocalBackupEnabled = true;
                    STS2Mobile.AppPaths.StoragePermissionAvailable = false;
                },
                CloudPushEligibilityBlockCode.BackupStoragePermissionMissing
            ),
        };

        foreach (var testCase in cases)
        {
            CloudBehaviorGateState.Reset();
            testCase.Configure();
            var view = new LauncherView();
            var coordinator = CreateCoordinator(view);
            var localBackupBeforeEvaluation =
                LauncherPreferences.LocalBackupEnabled;
            var result = coordinator.EvaluateCloudPushEligibility();

            AssertFalse(
                $"{testCase.Name} unexpectedly allows Upload",
                result.IsEligible
            );
            AssertEqual(
                $"{testCase.Name} blocker count",
                1,
                result.BlockingReasons.Count
            );
            AssertEqual(
                $"{testCase.Name} blocker code",
                testCase.ExpectedCode,
                result.BlockingReasons.Single().Code
            );
            AssertEqual(
                $"{testCase.Name} required action count",
                1,
                result.RequiredNextActions.Count
            );
            AssertEqual(
                $"{testCase.Name} status side effects",
                0,
                view.StatusMessages.Count
            );
            AssertEqual(
                $"{testCase.Name} log side effects",
                0,
                view.LogMessages.Count
            );
            AssertEqual(
                $"{testCase.Name} marker side effects",
                0,
                LauncherCloudSyncEvidence.BlockedReasons.Count
            );
            AssertEqual(
                $"{testCase.Name} opened an Upload confirmation",
                0,
                view.ConfirmationCount
            );
            AssertEqual(
                $"{testCase.Name} Local Backup state",
                localBackupBeforeEvaluation,
                LauncherPreferences.LocalBackupEnabled
            );
            AssertEqual(
                $"{testCase.Name} requested storage permission",
                0,
                STS2Mobile.AppPaths.RequestStoragePermissionCalls
            );
            AssertEqual(
                $"{testCase.Name} created storage directories",
                0,
                STS2Mobile.AppPaths.EnsureExternalDirectoriesCalls
            );
        }

        CloudBehaviorGateState.Reset();
        LauncherWorkshopModSafety.SelectedModCount = 3;
        LauncherCloudSyncEvidence.PullMatchesSelectedBranch = false;
        LauncherLocalSaveEvidence.ImportantSaveEvidenceAvailable = false;
        LauncherSaveOriginEvidence.MatchesSelectedRuntime = false;
        LauncherBranchSwitchSafety.MarkerPresent = true;
        LauncherBranchSwitchSafety.RequiredEvidenceAvailable = false;
        LauncherCloudSyncEvidence.PullAfterBranchSwitch = false;
        STS2Mobile.AppPaths.StoragePermissionAvailable = false;
        var aggregateView = new LauncherView();
        var aggregateResult = CreateCoordinator(
            aggregateView
        ).EvaluateCloudPushEligibility();

        AssertSequence(
            "aggregate coordinator blockers",
            new[]
            {
                CloudPushEligibilityBlockCode.ModsSelected,
                CloudPushEligibilityBlockCode.ManualPullVersionMismatch,
                CloudPushEligibilityBlockCode.ImportantLocalSavesMissing,
                CloudPushEligibilityBlockCode.LocalSaveOriginNotVerified,
                CloudPushEligibilityBlockCode.BranchSwitchEvidenceInvalid,
                CloudPushEligibilityBlockCode.ManualPullAfterBranchSwitchMissing,
                CloudPushEligibilityBlockCode.LocalBackupDisabledAfterBranchSwitch,
                CloudPushEligibilityBlockCode.BackupStoragePermissionMissing
            },
            aggregateResult.BlockingReasons.Select(block => block.Code).ToArray()
        );
        AssertEqual(
            "aggregate coordinator unique actions",
            7,
            aggregateResult.RequiredNextActions.Count
        );
        AssertEqual(
            "aggregate coordinator UI side effects",
            0,
            aggregateView.StatusMessages.Count + aggregateView.LogMessages.Count
        );

        _fixed++;
        Console.WriteLine(
            "[FIXED] Upload eligibility is structured, read-only, and UI-neutral."
        );
    }

    private static async Task VerifyStaleUploadApprovalIsBlockedAsync()
    {
        CloudBehaviorGateState.Reset();
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);
        var uploadStarted = false;

        AssertTrue(
            "initial Upload eligibility",
            coordinator.EvaluateCloudPushEligibility().IsEligible
        );

        coordinator.RequestSyntheticSafetyCheckedPush(
            "public",
            () => uploadStarted = true
        );
        AssertEqual(
            "overwrite confirmation count",
            1,
            view.ConfirmationCount
        );
        AssertFalse(
            "Upload ran before overwrite confirmation",
            uploadStarted
        );

        LauncherWorkshopModSafety.SelectedModCount = 1;
        view.ConfirmPending();
        await WaitUntilAsync(
            () => LauncherCloudSyncEvidence.BlockedReasons.Count == 1,
            "The stale Upload approval was not rejected."
        );

        AssertFalse(
            "stale approved Upload reached cloud work",
            uploadStarted
        );
        AssertFalse(
            "stale approved Upload left controls disabled",
            view.PushPullDisabled
        );
        AssertEqual(
            "stale approved Upload blocked marker count",
            1,
            LauncherCloudSyncEvidence.BlockedReasons.Count
        );
        AssertContains(
            "stale approved Upload status",
            view.StatusMessages.Last(),
            "blocked before any Steam Cloud write"
        );

        CloudBehaviorGateState.Reset();
        uploadStarted = false;
        coordinator.RequestSyntheticSafetyCheckedPush(
            "public",
            () => uploadStarted = true
        );
        LauncherPreferences.SelectedBranch = "public-beta";
        view.ConfirmPending();
        await WaitUntilAsync(
            () => LauncherCloudSyncEvidence.BlockedReasons.Count == 1,
            "The changed-version Upload approval was not rejected."
        );
        AssertFalse(
            "changed-version approved Upload reached cloud work",
            uploadStarted
        );
        AssertContains(
            "changed-version Upload status",
            view.StatusMessages.Last(),
            "selected game version changed"
        );
    }

    private static async Task VerifyDuplicateCloudRequestsAreRejectedAsync()
    {
        CloudBehaviorGateState.Reset();
        var release = NewCompletionSource<string>();
        var entered = NewCompletionSource<bool>();
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);
        var runCalls = 0;

        async Task<string> Run(CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref runCalls);
            entered.TrySetResult(true);
            return await release.Task.WaitAsync(cancellationToken);
        }

        coordinator.RequestSyntheticSync("Pull", Run);
        await entered.Task;
        coordinator.RequestSyntheticSync("Pull", Run);

        await Task.Delay(25);
        AssertEqual(
            "operation calls after duplicate tap",
            1,
            Volatile.Read(ref runCalls)
        );
        AssertEqual(
            "credential refreshes after duplicate taps",
            1,
            coordinator.ModelForTest.RefreshCloudSaveCredentialsCalls
        );
        AssertContains(
            "duplicate rejection status",
            view.StatusMessages.Last(),
            "already running"
        );
        AssertTrue("operation gate remains active", coordinator.IsOperationActive);
        AssertTrue("controls remain disabled", view.PushPullDisabled);

        release.SetResult("download complete");
        await WaitUntilAsync(
            () => !coordinator.IsOperationActive,
            "accepted Pull operation did not finish"
        );
        AssertFalse(
            "controls remain disabled after operation completion",
            view.PushPullDisabled
        );
    }

    private static async Task VerifyPullRefreshesEligibilityBeforeControlsAsync()
    {
        CloudBehaviorGateState.Reset();
        LauncherBackupEvidence.CurrentMirrorCount = 2;
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);

        await coordinator.ExecuteSyntheticRequestAsync(
            "Pull",
            _ => Task.FromResult("download complete")
        );

        var snapshot = view.PostOperationSnapshots.Last();
        AssertTrue(
            "refreshed Upload eligibility",
            snapshot.UploadEligibility.IsEligible
        );
        AssertEqual(
            "refreshed important save count",
            1,
            snapshot.ImportantLocalSaveEvidenceCount
        );
        AssertEqual(
            "refreshed mirror count",
            2,
            snapshot.CurrentMirrorSaveCount
        );
        AssertSequence(
            "snapshot precedes control restoration",
            new[]
            {
                "disabled:True",
                "snapshot:eligible=True",
                "disabled:False",
            },
            view.UiEvents.ToArray()
        );
        AssertContains(
            "immediate Upload unlock summary",
            view.StatusMessages.Last(),
            "Upload is now available"
        );
    }

    private static Task VerifyBackupRecoveryRefreshesStateAsync()
    {
        CloudBehaviorGateState.Reset();
        LauncherBackupEvidence.CurrentMirrorCount = 4;
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);
        var result = new LocalBackupRefreshResult(
            Attempted: true,
            StorageAccessAvailable: true,
            Discovered: 3,
            Mirrored: 2,
            Archived: 1,
            Restored: 2,
            Errors: 0,
            FailureMessage: ""
        );

        coordinator.LocalBackupRecoveryCompleted(result);

        var snapshot = view.PostOperationSnapshots.Last();
        AssertEqual(
            "recovery mirror recapture",
            4,
            snapshot.CurrentMirrorSaveCount
        );
        AssertTrue(
            "recovery Upload eligibility",
            snapshot.UploadEligibility.IsEligible
        );
        AssertContains(
            "recovery summary",
            view.StatusMessages.Last(),
            "Save recovery succeeded"
        );
        return Task.CompletedTask;
    }

    private static async Task VerifyTimeoutCancelsAndDrainsAsync()
    {
        CloudBehaviorGateState.Reset();
        var entered = NewCompletionSource<bool>();
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);
        var cancellationObserved = false;
        var cleanupCompleted = false;
        var lateMutation = false;

        var execution = coordinator.ExecuteSyntheticRequestAsync(
            "Pull",
            async cancellationToken =>
            {
                entered.TrySetResult(true);
                try
                {
                    await Task.Delay(
                        Timeout.Infinite,
                        cancellationToken
                    );
                    lateMutation = true;
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    cancellationObserved = true;
                    throw;
                }
                finally
                {
                    await Task.Delay(20);
                    cleanupCompleted = true;
                }

                return "late local-save write";
            },
            timeoutMs: 25
        );

        await entered.Task;
        await execution;

        AssertTrue("timeout cancellation reached worker", cancellationObserved);
        AssertTrue("worker cleanup completed before timeout returned", cleanupCompleted);
        AssertFalse("worker performed a late mutation", lateMutation);
        AssertFalse(
            "UI controls remain disabled after timeout",
            view.PushPullDisabled
        );
        AssertContains(
            "timeout status",
            view.StatusMessages.Last(),
            "Pull timed out"
        );
        AssertFalse("operation gate remains active after timeout", coordinator.IsOperationActive);
    }

    private static async Task VerifyLifecycleDisposalAsync()
    {
        CloudBehaviorGateState.Reset();
        var entered = NewCompletionSource<bool>();
        var cleanupCompleted = false;
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);
        var execution = coordinator.ExecuteSyntheticRequestAsync(
            "Pull",
            async cancellationToken =>
            {
                try
                {
                    entered.TrySetResult(true);
                    await Task.Delay(
                        Timeout.Infinite,
                        cancellationToken
                    );
                }
                finally
                {
                    await Task.Delay(20);
                    cleanupCompleted = true;
                }

                return "";
            }
        );

        await entered.Task;
        var statusCountBeforeDisposal = view.StatusMessages.Count;
        coordinator.Dispose();
        await execution;

        AssertTrue(
            "coordinator disposal returned before worker cleanup",
            cleanupCompleted
        );
        AssertFalse(
            "operation remained active after coordinator disposal",
            coordinator.IsOperationActive
        );
        AssertEqual(
            "UI updates were queued after coordinator disposal",
            statusCountBeforeDisposal,
            view.StatusMessages.Count
        );
    }

    private static async Task VerifyExplicitCancellationAsync()
    {
        CloudBehaviorGateState.Reset();
        var entered = NewCompletionSource<bool>();
        var cancellationObserved = false;
        var cleanupCompleted = false;
        var view = new LauncherView();
        var coordinator = CreateCoordinator(view);
        var execution = coordinator.ExecuteSyntheticRequestAsync(
            "Pull",
            async cancellationToken =>
            {
                try
                {
                    entered.TrySetResult(true);
                    await Task.Delay(
                        Timeout.Infinite,
                        cancellationToken
                    );
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    cancellationObserved = true;
                    throw;
                }
                finally
                {
                    await Task.Delay(20);
                    cleanupCompleted = true;
                }

                return "";
            }
        );

        await entered.Task;
        coordinator.CloudOperationCancelPressed();
        await execution;

        AssertTrue("explicit cancellation reached worker", cancellationObserved);
        AssertTrue("explicit cancellation drained cleanup", cleanupCompleted);
        AssertFalse("gate remains active after explicit cancellation", coordinator.IsOperationActive);
        AssertFalse("controls remain disabled after explicit cancellation", view.PushPullDisabled);
        AssertContains(
            "explicit cancellation status",
            view.StatusMessages.Last(),
            "cancelled"
        );
    }

    private static LauncherCloudSyncCoordinator CreateCoordinator(
        LauncherView view
    )
    {
        var model = new LauncherModel("test-data");
        var coordinator = new LauncherCloudSyncCoordinator(
            model,
            view,
            action => action()
        );
        coordinator.ModelForTest = model;
        return coordinator;
    }

    private static TaskCompletionSource<T> NewCompletionSource<T>()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static async Task WaitUntilAsync(
        Func<bool> predicate,
        string failureMessage
    )
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!predicate())
        {
            if (DateTime.UtcNow >= deadline)
                throw new InvalidOperationException(failureMessage);

            await Task.Delay(10);
        }
    }

    private static void AssertTrue(string name, bool actual)
    {
        if (!actual)
            throw new InvalidOperationException($"{name}: expected true.");
    }

    private static void AssertFalse(string name, bool actual)
    {
        if (actual)
            throw new InvalidOperationException($"{name}: expected false.");
    }

    private static void AssertEqual<T>(string name, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected {expected}, actual {actual}."
            );
        }
    }

    private static void AssertContains(
        string name,
        string actual,
        string expected
    )
    {
        if (!actual.Contains(expected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{name}: expected '{expected}' in '{actual}'."
            );
        }
    }

    private static void AssertSequence<T>(
        string name,
        IReadOnlyCollection<T> expected,
        IReadOnlyCollection<T> actual
    )
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"{name}: expected [{string.Join(", ", expected)}], "
                    + $"actual [{string.Join(", ", actual)}]."
            );
        }
    }
}

internal sealed partial class LauncherCloudSyncCoordinator
{
    internal LauncherModel ModelForTest { get; set; } = null!;
}
