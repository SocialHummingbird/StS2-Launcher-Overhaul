using STS2Mobile.Launcher;

var failures = new List<string>();
long now = 1_000;
var deadline = LauncherMonotonicDeadline.ForTest(
    TimeSpan.FromMilliseconds(100),
    () => now
);

Expect(!deadline.IsExpired, "a new deadline should remain active");
Expect(deadline.ElapsedMilliseconds == 0, "elapsed time should start at zero");
Expect(deadline.RemainingDelayMilliseconds == 100, "the full budget should remain");

Expect(
    LauncherThreadedLoadPolicy.EvaluateRequest(true, false, deadline.IsExpired)
        == LauncherThreadedLoadDecision.ContinuePolling,
    "an accepted request should enter cooperative polling"
);
Expect(
    LauncherThreadedLoadPolicy.EvaluateRequest(false, true, deadline.IsExpired)
        == LauncherThreadedLoadDecision.ContinuePolling,
    "an already-running request should be adopted"
);
Expect(
    LauncherThreadedLoadPolicy.EvaluateRequest(false, false, deadline.IsExpired)
        == LauncherThreadedLoadDecision.Fail,
    "a rejected request should fail without polling"
);
Expect(
    LauncherThreadedLoadPolicy.EvaluatePoll(
        LauncherThreadedLoadPollState.Loaded,
        deadline.IsExpired
    ) == LauncherThreadedLoadDecision.Complete,
    "only a loaded resource should become retrievable"
);
Expect(
    LauncherThreadedLoadPolicy.EvaluatePoll(
        LauncherThreadedLoadPollState.Failed,
        deadline.IsExpired
    ) == LauncherThreadedLoadDecision.Fail,
    "a failed background load should continue startup without retrieval"
);
Expect(
    LauncherThreadedLoadPolicy.EvaluatePoll(
        LauncherThreadedLoadPollState.InvalidResource,
        deadline.IsExpired
    ) == LauncherThreadedLoadDecision.Fail,
    "an invalid resource should continue startup without retrieval"
);

for (var poll = 0; poll < 4; poll++)
{
    Expect(
        LauncherThreadedLoadPolicy.EvaluatePoll(
            LauncherThreadedLoadPollState.InProgress,
            deadline.IsExpired
        ) == LauncherThreadedLoadDecision.ContinuePolling,
        "an in-progress load should yield and poll again before the deadline"
    );
    now += 20;
}

now = 1_099;
Expect(!deadline.IsExpired, "the deadline should remain active one millisecond early");
Expect(deadline.RemainingDelayMilliseconds == 1, "the final millisecond should be bounded");

now = 1_100;
Expect(deadline.IsExpired, "the deadline should expire at the exact boundary");
Expect(deadline.RemainingDelayMilliseconds == 0, "no delay should remain after expiry");
Expect(
    LauncherThreadedLoadPolicy.EvaluatePoll(
        LauncherThreadedLoadPollState.InProgress,
        deadline.IsExpired
    ) == LauncherThreadedLoadDecision.DeadlineExceeded,
    "a stalled load should be abandoned at the overall deadline"
);
Expect(
    LauncherThreadedLoadPolicy.EvaluatePoll(
        LauncherThreadedLoadPollState.Loaded,
        deadline.IsExpired
    ) == LauncherThreadedLoadDecision.DeadlineExceeded,
    "expiry should win even when completion races the hard boundary"
);

var presentation = new ShaderWarmupPresentationLifecycle();
Expect(presentation.MarkVisible(), "the warmup presentation should become visible");
Expect(
    presentation.TryBeginCleanup(),
    "deadline completion should begin presentation cleanup"
);
Expect(
    !presentation.WarmupVisible && presentation.StartupCoverRetained,
    "cleanup should reveal the retained startup cover"
);
Expect(presentation.InputBlocked, "the startup handoff should remain input-blocked");
Expect(!presentation.TryBeginCleanup(), "cleanup should remain idempotent");

if (failures.Count > 0)
{
    Console.Error.WriteLine("Shader-warmup bounded-loading policy probe failed:");
    foreach (var failure in failures)
        Console.Error.WriteLine($"- {failure}");
    return 1;
}

Console.WriteLine(
    "Shader-warmup bounded-loading policy probe passed "
    + "(success, queue failure, load failure, stalled polling, hard deadline, cleanup)."
);
return 0;

void Expect(bool condition, string message)
{
    if (!condition)
        failures.Add(message);
}
