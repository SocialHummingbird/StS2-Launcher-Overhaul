using STS2Mobile.Launcher;
using STS2Mobile.Patches;

var probe = new StartupProductionPathProbe();
await probe.RunAsync();
return probe.Complete();

internal sealed class StartupProductionPathProbe
{
    private readonly List<string> _failures = new();

    internal async Task RunAsync()
    {
        await VerifyThreadedLoadingAsync();
        await VerifyLifecycleAwareSignalsAsync();
        await VerifyRenderedFrameHandoffAsync();
        await VerifyWarmupPresentationAsync();
        VerifyDiagnosticsGating();
        VerifyResourcePackInvalidation();
        VerifyDeadlineNesting();
    }

    internal int Complete()
    {
        if (_failures.Count > 0)
        {
            Console.Error.WriteLine("Startup production-path probe failed:");
            foreach (var failure in _failures)
                Console.Error.WriteLine($"- {failure}");
            return 1;
        }

        Console.WriteLine(
            "Startup production-path probe passed "
            + "(successful/slow/stalled/failed/hung threaded loads, hard deadlines, "
            + "pause/resume and destruction, FramePostDraw handoff, warmup layering, "
            + "diagnostic opt-in, resource-pack invalidation, stale cache publication)."
        );
        return 0;
    }

    private async Task VerifyThreadedLoadingAsync()
    {
        long now = 0;
        var successSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        successSignals.EnqueueProcessFrame(advanceMilliseconds: 12);
        var successResources = new FakeThreadedResourceApi(
            LauncherThreadedLoadPollState.InProgress,
            LauncherThreadedLoadPollState.Loaded
        );
        var success = await LauncherThreadedResourceLoadOperation.RunAsync(
            successResources,
            new LauncherAsyncSignalWaiter(successSignals),
            "res://success.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            success.Outcome == LauncherThreadedResourceOperationOutcome.Loaded
                && success.Resource == FakeThreadedResourceApi.DefaultResource,
            "the production threaded-load operation should retrieve a completed resource"
        );
        Expect(
            successResources.RequestCount == 1
                && successResources.PollCount == 2
                && successResources.RetrieveCount == 1
                && successSignals.ProcessFrameCalls == 1,
            "successful threaded loading should request once and cooperatively poll through ProcessFrame"
        );

        now = 0;
        var slowSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        slowSignals.EnqueueProcessFrame(advanceMilliseconds: 20);
        slowSignals.EnqueueProcessFrame(advanceMilliseconds: 20);
        slowSignals.EnqueueProcessFrame(advanceMilliseconds: 20);
        var slow = await LauncherThreadedResourceLoadOperation.RunAsync(
            new FakeThreadedResourceApi(
                LauncherThreadedLoadPollState.InProgress,
                LauncherThreadedLoadPollState.InProgress,
                LauncherThreadedLoadPollState.InProgress,
                LauncherThreadedLoadPollState.Loaded
            ),
            new LauncherAsyncSignalWaiter(slowSignals),
            "res://slow.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            slow.Outcome == LauncherThreadedResourceOperationOutcome.Loaded && now == 60,
            "a slow load should keep yielding and complete inside its overall deadline"
        );

        now = 0;
        var rejectedResources = new FakeThreadedResourceApi
        {
            RequestState = LauncherThreadedRequestState.Rejected,
        };
        var rejected = await LauncherThreadedResourceLoadOperation.RunAsync(
            rejectedResources,
            new LauncherAsyncSignalWaiter(
                FakeSignalSource.WithClock(() => now, value => now = value)
            ),
            "res://rejected.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            rejected.Outcome == LauncherThreadedResourceOperationOutcome.Failed
                && rejectedResources.PollCount == 0,
            "a rejected threaded request should fail without entering the polling loop"
        );

        var failedResources = new FakeThreadedResourceApi(
            LauncherThreadedLoadPollState.Failed
        );
        var failed = await LauncherThreadedResourceLoadOperation.RunAsync(
            failedResources,
            new LauncherAsyncSignalWaiter(
                FakeSignalSource.WithClock(() => now, value => now = value)
            ),
            "res://failed.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            failed.Outcome == LauncherThreadedResourceOperationOutcome.Failed
                && failedResources.RetrieveCount == 0,
            "a failed background load should never call the retrieval API"
        );

        var emptyResources = new FakeThreadedResourceApi(
            LauncherThreadedLoadPollState.Loaded
        )
        {
            Resource = null,
        };
        var empty = await LauncherThreadedResourceLoadOperation.RunAsync(
            emptyResources,
            new LauncherAsyncSignalWaiter(
                FakeSignalSource.WithClock(() => now, value => now = value)
            ),
            "res://empty.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            empty.Outcome == LauncherThreadedResourceOperationOutcome.Failed,
            "a loaded status without a resource should remain a classified failure"
        );

        now = 0;
        var hungSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        hungSignals.CompleteTimeouts = true;
        hungSignals.EnqueueHungProcessFrame();
        var hung = await LauncherThreadedResourceLoadOperation.RunAsync(
            new FakeThreadedResourceApi(LauncherThreadedLoadPollState.InProgress),
            new LauncherAsyncSignalWaiter(hungSignals),
            "res://hung.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            hung.Outcome == LauncherThreadedResourceOperationOutcome.DeadlineExceeded
                && now == 100,
            "a hung ProcessFrame signal should hand off at the hard monotonic deadline"
        );

        now = 0;
        var stalledSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        for (var index = 0; index < 4; index++)
            stalledSignals.EnqueueProcessFrame(advanceMilliseconds: 25);
        var stalled = await LauncherThreadedResourceLoadOperation.RunAsync(
            new FakeThreadedResourceApi(LauncherThreadedLoadPollState.InProgress),
            new LauncherAsyncSignalWaiter(stalledSignals),
            "res://stalled.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            stalled.Outcome == LauncherThreadedResourceOperationOutcome.DeadlineExceeded
                && stalledSignals.ProcessFrameCalls == 4,
            "a responsive but permanently stalled load should stop polling at the overall deadline"
        );

        now = 0;
        var boundarySignals = FakeSignalSource.WithClock(() => now, value => now = value);
        boundarySignals.EnqueueProcessFrame(advanceMilliseconds: 100);
        var boundary = await LauncherThreadedResourceLoadOperation.RunAsync(
            new FakeThreadedResourceApi(
                LauncherThreadedLoadPollState.InProgress,
                LauncherThreadedLoadPollState.Loaded
            ),
            new LauncherAsyncSignalWaiter(boundarySignals),
            "res://boundary.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            boundary.Outcome == LauncherThreadedResourceOperationOutcome.DeadlineExceeded,
            "deadline expiry should win when a frame arrives exactly at the hard boundary"
        );
    }

    private async Task VerifyLifecycleAwareSignalsAsync()
    {
        long now = 0;
        var lifecycle = new LauncherOperationLifecycle();
        var pausedSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        pausedSignals.EnqueueHungProcessFrame(() =>
        {
            lifecycle.Pause();
        });
        pausedSignals.EnqueueProcessFrame(advanceMilliseconds: 10);
        pausedSignals.EnqueueProcessFrame(advanceMilliseconds: 10);
        var pausedResources = new FakeThreadedResourceApi(
            LauncherThreadedLoadPollState.InProgress,
            LauncherThreadedLoadPollState.InProgress,
            LauncherThreadedLoadPollState.Loaded
        );
        var pausedTask = LauncherThreadedResourceLoadOperation.RunAsync(
            pausedResources,
            new LauncherAsyncSignalWaiter(pausedSignals, lifecycle),
            "res://resume.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        await Task.Yield();
        Expect(
            lifecycle.Capture().State == LauncherOperationLifecycleState.Paused
                && !pausedTask.IsCompleted,
            "Home/pause should suspend a pending production load without publishing a result"
        );
        Expect(lifecycle.Resume(), "resume should reactivate a paused startup operation");
        var resumed = await CompleteWithinAsync(pausedTask, "resumed threaded load");
        Expect(
            resumed.Outcome == LauncherThreadedResourceOperationOutcome.Loaded,
            "the same production load should continue after Home/resume"
        );

        now = 0;
        var destroyedLifecycle = new LauncherOperationLifecycle();
        var destroyedSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        destroyedSignals.EnqueueHungProcessFrame(() =>
        {
            destroyedLifecycle.Destroy();
        });
        var destroyed = await LauncherThreadedResourceLoadOperation.RunAsync(
            new FakeThreadedResourceApi(LauncherThreadedLoadPollState.InProgress),
            new LauncherAsyncSignalWaiter(destroyedSignals, destroyedLifecycle),
            "res://destroyed.tres",
            string.Empty,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now)
        );
        Expect(
            destroyed.Outcome == LauncherThreadedResourceOperationOutcome.Cancelled,
            "scene-tree destruction should cancel a hung load instead of waiting for its deadline"
        );
        Expect(
            !destroyedLifecycle.Destroy() && !destroyedLifecycle.Resume(),
            "destruction cleanup should latch and reject later lifecycle transitions"
        );
    }

    private async Task VerifyRenderedFrameHandoffAsync()
    {
        long now = 0;
        var frameSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        frameSignals.EnqueueFramePostDraw(advanceMilliseconds: 20);
        frameSignals.EnqueueFramePostDraw(advanceMilliseconds: 20);
        frameSignals.EnqueueFramePostDraw(advanceMilliseconds: 20);
        var stableTracker = new MainMenuFrameStabilityTracker(3, 30, 60, 100);
        var stable = await MainMenuRenderedFrameHandoff.WaitAsync(
            stableTracker,
            new LauncherAsyncSignalWaiter(frameSignals),
            () => true,
            () => now,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now),
            100
        );
        Expect(
            stable.Result.CanExposeMainMenu
                && stableTracker.FramePostDrawSamples == 3
                && frameSignals.FramePostDrawCalls == 3,
            "three stable FramePostDraw samples should admit the main-menu handoff"
        );
        Expect(
            frameSignals.ProcessFrameCalls == 0,
            "ProcessFrame must not substitute for rendered FramePostDraw evidence"
        );

        now = 0;
        var timeoutSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        timeoutSignals.CompleteTimeouts = true;
        timeoutSignals.EnqueueHungFramePostDraw();
        var timeoutTracker = new MainMenuFrameStabilityTracker(2, 30, 0, 100);
        var timedOut = await MainMenuRenderedFrameHandoff.WaitAsync(
            timeoutTracker,
            new LauncherAsyncSignalWaiter(timeoutSignals),
            () => true,
            () => now,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now),
            100
        );
        Expect(
            timedOut.Result.Outcome == AndroidMainMenuPreparationOutcome.TimedOut
                && timeoutTracker.Outcome == MainMenuFrameStabilityOutcome.TimedOut,
            "a missing FramePostDraw signal should produce the recovery handoff at timeout"
        );

        now = 0;
        var targetAlive = true;
        var destroyedLifecycle = new LauncherOperationLifecycle();
        var destroyedSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        destroyedSignals.EnqueueHungFramePostDraw(() =>
        {
            targetAlive = false;
            destroyedLifecycle.Destroy();
        });
        var abortedTracker = new MainMenuFrameStabilityTracker(2, 30, 0, 100);
        var aborted = await MainMenuRenderedFrameHandoff.WaitAsync(
            abortedTracker,
            new LauncherAsyncSignalWaiter(destroyedSignals, destroyedLifecycle),
            () => targetAlive,
            () => now,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now),
            100
        );
        Expect(
            aborted.Result.Outcome == AndroidMainMenuPreparationOutcome.Aborted
                && abortedTracker.Outcome == MainMenuFrameStabilityOutcome.Aborted,
            "scene destruction during FramePostDraw should abort and block menu exposure"
        );

        now = 0;
        var resumedLifecycle = new LauncherOperationLifecycle();
        var resumedSignals = FakeSignalSource.WithClock(() => now, value => now = value);
        resumedSignals.EnqueueHungFramePostDraw(() =>
        {
            resumedLifecycle.Pause();
        });
        resumedSignals.EnqueueFramePostDraw(advanceMilliseconds: 20);
        resumedSignals.EnqueueFramePostDraw(advanceMilliseconds: 20);
        var resumedTracker = new MainMenuFrameStabilityTracker(2, 30, 0, 100);
        var resumedTask = MainMenuRenderedFrameHandoff.WaitAsync(
            resumedTracker,
            new LauncherAsyncSignalWaiter(resumedSignals, resumedLifecycle),
            () => true,
            () => now,
            LauncherMonotonicDeadline.ForTest(TimeSpan.FromMilliseconds(100), () => now),
            100
        );
        await Task.Yield();
        Expect(!resumedTask.IsCompleted, "paused rendered-frame observation should remain pending");
        resumedLifecycle.Resume();
        var resumed = await CompleteWithinAsync(resumedTask, "resumed FramePostDraw handoff");
        Expect(
            resumed.Result.CanExposeMainMenu && resumedTracker.FramePostDrawSamples == 2,
            "Home/resume should discard the interrupted sample and require fresh rendered frames"
        );
    }

    private async Task VerifyWarmupPresentationAsync()
    {
        Expect(
            StartupPresentationLayerPolicy.HasValidOrdering
                && StartupPresentationLayerPolicy.ShaderWarmupCanvasLayer
                    > StartupPresentationLayerPolicy.StartupStatusCanvasLayer,
            "shader warmup should remain above the retained startup cover"
        );

        var lifecycle = new ShaderWarmupPresentationLifecycle();
        var events = new List<string>();
        Expect(lifecycle.MarkVisible(), "warmup should become visible before work starts");
        await ShaderWarmupPresentationRun.ExecuteAsync(
            () =>
            {
                events.Add("run");
                Expect(
                    lifecycle.WarmupVisible && lifecycle.InputBlocked,
                    "warmup work should run while its visible layer blocks input"
                );
                return Task.CompletedTask;
            },
            () =>
            {
                events.Add("cleanup");
                return lifecycle.TryBeginCleanup();
            }
        );
        Expect(
            events.SequenceEqual(new[] { "run", "cleanup" }),
            "presentation cleanup should occur strictly after warmup execution"
        );
        Expect(
            !lifecycle.WarmupVisible
                && lifecycle.StartupCoverRetained
                && lifecycle.InputBlocked,
            "warmup cleanup should reveal the retained input-blocking startup cover"
        );

        var failedLifecycle = new ShaderWarmupPresentationLifecycle();
        failedLifecycle.MarkVisible();
        var cleanupCalled = false;
        try
        {
            await ShaderWarmupPresentationRun.ExecuteAsync(
                () => Task.FromException(new InvalidOperationException("probe")),
                () =>
                {
                    cleanupCalled = true;
                    return failedLifecycle.TryBeginCleanup();
                }
            );
        }
        catch (InvalidOperationException)
        {
        }

        Expect(
            cleanupCalled && !failedLifecycle.WarmupVisible,
            "warmup presentation cleanup should run when production execution fails"
        );
    }

    private void VerifyDiagnosticsGating()
    {
        Expect(
            !PostStartupDiagnosticsConfiguration.DetailedTraceEnabled(
                new FakeDiagnosticsSource(string.Empty, markerExists: false)
            ),
            "ordinary startup diagnostics should remain lightweight by default"
        );
        Expect(
            PostStartupDiagnosticsConfiguration.DetailedTraceEnabled(
                new FakeDiagnosticsSource("true", markerExists: false)
            ),
            "the production diagnostics configuration should honor environment opt-in"
        );
        Expect(
            PostStartupDiagnosticsConfiguration.DetailedTraceEnabled(
                new FakeDiagnosticsSource(string.Empty, markerExists: true)
            ),
            "the production diagnostics configuration should honor marker opt-in"
        );
        Expect(
            !PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(false, false)
                && PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(false, true)
                && PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(true, false),
            "full traces should be gated to explicit opt-in, failure, and recovery paths"
        );

        var schedule = new PostStartupDiagnosticsScheduleGate();
        Expect(
            schedule.TrySchedule() && schedule.Stop() && schedule.IsStopped,
            "diagnostic lifecycle cleanup should stop an active production schedule"
        );
        Expect(
            !schedule.TrySchedule() && !schedule.Stop(),
            "diagnostic cleanup should prevent duplicate scheduling after scene destruction"
        );
    }

    private void VerifyResourcePackInvalidation()
    {
        var cache = new AndroidAtlasFallbackResolutionCache();
        var tracker = new AndroidAtlasResourceChangeTracker(cache);
        long generation = cache.CaptureGeneration();
        cache.TryStoreHit("atlas/hit", "texture/old", generation);
        cache.TryStoreMiss("atlas/miss", generation);

        var standardMod = tracker.RecordResourcePackLoad(true, true);
        Expect(
            standardMod.Invalidated && standardMod.RemovedCount == 2,
            "a standard mod pack should invalidate positive and negative atlas entries"
        );
        Expect(
            !cache.TryStoreMiss("atlas/stale-miss", generation)
                && !cache.TryStoreHit("atlas/stale-hit", "texture/stale", generation),
            "stale positive and negative publications should fail after pack invalidation"
        );

        generation = cache.CaptureGeneration();
        cache.TryStoreMiss("atlas/baselib", generation);
        Expect(
            tracker.RecordResourcePackLoad(true, true).Invalidated,
            "BaseLib should use the same global resource-pack invalidation boundary"
        );
        generation = cache.CaptureGeneration();
        cache.TryStoreMiss("atlas/late", generation);
        Expect(
            tracker.RecordResourcePackLoad(true, true).Invalidated,
            "future late-loaded packs should use the same invalidation boundary"
        );

        var identityCache = new AndroidAtlasFallbackResolutionCache();
        var identityTracker = new AndroidAtlasResourceChangeTracker(identityCache);
        identityTracker.ObserveMountedResourceSet("public|aaaaaaaaaaaa|vanilla");
        var identityGeneration = identityCache.CaptureGeneration();
        identityCache.TryStoreMiss("atlas/branch", identityGeneration);
        var branch = identityTracker.ObserveMountedResourceSet(
            "public-beta|bbbbbbbbbbbb|modded"
        );
        Expect(
            branch.Invalidated && identityCache.Count == 0,
            "branch, PCK, and mod-mode identity changes should clear stale misses"
        );
        Expect(
            !identityTracker.RecordResourcePackLoad(false, true).Invalidated,
            "failed resource-pack loads should preserve the current generation"
        );
    }

    private void VerifyDeadlineNesting()
    {
        long now = 1_000;
        var parent = LauncherMonotonicDeadline.ForTest(
            TimeSpan.FromMilliseconds(100),
            () => now
        );
        now += 25;
        var child = parent.CreateChild(TimeSpan.FromMilliseconds(500));
        Expect(
            child.RemainingDelayMilliseconds == 75,
            "a production child deadline should never outlive its parent"
        );
        now += 75;
        Expect(
            parent.IsExpired && child.IsExpired,
            "parent and child deadlines should expire at the same hard monotonic boundary"
        );
    }

    private async Task<T> CompleteWithinAsync<T>(Task<T> task, string name)
    {
        var completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2)));
        if (completed != task)
        {
            _failures.Add($"{name} did not complete after its lifecycle resumed");
            throw new TimeoutException(name);
        }

        return await task;
    }

    private void Expect(bool condition, string message)
    {
        if (!condition)
            _failures.Add(message);
    }
}

internal sealed class FakeThreadedResourceApi : ILauncherThreadedResourceApi<string>
{
    internal const string DefaultResource = "resource";
    private readonly Queue<LauncherThreadedLoadPollState> _polls;
    private LauncherThreadedLoadPollState _lastPoll =
        LauncherThreadedLoadPollState.InProgress;

    internal FakeThreadedResourceApi(
        params LauncherThreadedLoadPollState[] polls
    )
    {
        _polls = new Queue<LauncherThreadedLoadPollState>(polls);
    }

    internal LauncherThreadedRequestState RequestState { get; set; } =
        LauncherThreadedRequestState.Accepted;
    internal string Resource { get; set; } = DefaultResource;
    internal int RequestCount { get; private set; }
    internal int PollCount { get; private set; }
    internal int RetrieveCount { get; private set; }

    public LauncherThreadedRequestState Request(string path, string typeHint)
    {
        RequestCount++;
        return RequestState;
    }

    public LauncherThreadedLoadPollState Poll(string path)
    {
        PollCount++;
        if (_polls.Count > 0)
            _lastPoll = _polls.Dequeue();
        return _lastPoll;
    }

    public string Retrieve(string path)
    {
        RetrieveCount++;
        return Resource;
    }
}

internal sealed class FakeSignalSource : ILauncherAsyncSignalSource
{
    private static readonly Task Never = new TaskCompletionSource<bool>().Task;
    private readonly Func<long> _clock;
    private readonly Action<long> _setClock;
    private readonly Queue<SignalStep> _processFrames = new();
    private readonly Queue<SignalStep> _framePostDraws = new();

    private FakeSignalSource(Func<long> clock, Action<long> setClock)
    {
        _clock = clock;
        _setClock = setClock;
    }

    internal bool CompleteTimeouts { get; set; }
    internal int ProcessFrameCalls { get; private set; }
    internal int FramePostDrawCalls { get; private set; }

    internal static FakeSignalSource WithClock(
        Func<long> clock,
        Action<long> setClock
    )
        => new(clock, setClock);

    internal void EnqueueProcessFrame(long advanceMilliseconds = 0)
        => _processFrames.Enqueue(SignalStep.Completed(advanceMilliseconds));

    internal void EnqueueHungProcessFrame(Action action = null)
        => _processFrames.Enqueue(SignalStep.Hung(action));

    internal void EnqueueFramePostDraw(long advanceMilliseconds = 0)
        => _framePostDraws.Enqueue(SignalStep.Completed(advanceMilliseconds));

    internal void EnqueueHungFramePostDraw(Action action = null)
        => _framePostDraws.Enqueue(SignalStep.Hung(action));

    public Task WaitForProcessFrameAsync()
    {
        ProcessFrameCalls++;
        return Take(_processFrames);
    }

    public Task WaitForFramePostDrawAsync()
    {
        FramePostDrawCalls++;
        return Take(_framePostDraws);
    }

    public Task WaitForDelayAsync(
        int milliseconds,
        CancellationToken cancellationToken
    )
    {
        if (!CompleteTimeouts)
            return Never;

        Advance(milliseconds);
        return Task.CompletedTask;
    }

    private Task Take(Queue<SignalStep> steps)
    {
        if (steps.Count == 0)
            return Never;

        var step = steps.Dequeue();
        step.Action?.Invoke();
        Advance(step.AdvanceMilliseconds);
        return step.Completes ? Task.CompletedTask : Never;
    }

    private void Advance(long milliseconds)
        => _setClock(checked(_clock() + Math.Max(0, milliseconds)));

    private readonly struct SignalStep
    {
        private SignalStep(bool completes, long advanceMilliseconds, Action action)
        {
            Completes = completes;
            AdvanceMilliseconds = advanceMilliseconds;
            Action = action;
        }

        internal bool Completes { get; }
        internal long AdvanceMilliseconds { get; }
        internal Action Action { get; }

        internal static SignalStep Completed(long advanceMilliseconds)
            => new(true, advanceMilliseconds, null);

        internal static SignalStep Hung(Action action)
            => new(false, 0, action);
    }
}

internal sealed class FakeDiagnosticsSource :
    IPostStartupDiagnosticsConfigurationSource
{
    internal FakeDiagnosticsSource(string environmentValue, bool markerExists)
    {
        EnvironmentValue = environmentValue;
        MarkerExists = markerExists;
    }

    public string EnvironmentValue { get; }
    public bool MarkerExists { get; }
}
