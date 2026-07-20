using STS2Mobile.Launcher;
using STS2Mobile.Patches;

var failures = new List<string>();

Expect(
    !PostStartupDiagnosticsPolicy.DetailedTraceEnabled(string.Empty, markerExists: false),
    "detailed post-startup traces should be disabled by default"
);
Expect(
    PostStartupDiagnosticsPolicy.DetailedTraceEnabled(string.Empty, markerExists: true),
    "the explicit marker should enable detailed post-startup traces"
);
foreach (var enabled in new[] { "1", "true", "YES", "on", " enabled " })
{
    Expect(
        PostStartupDiagnosticsPolicy.DetailedTraceEnabled(enabled, markerExists: false),
        $"environment value '{enabled}' should enable detailed traces"
    );
}
foreach (var disabled in new[] { "", "0", "false", "verbose", "disabled" })
{
    Expect(
        !PostStartupDiagnosticsPolicy.DetailedTraceEnabled(disabled, markerExists: false),
        $"environment value '{disabled}' should not enable detailed traces"
    );
}
Expect(
    !PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(
        detailedTraceEnabled: false,
        failureOrRecovery: false
    ),
    "ordinary successful startup should use lightweight diagnostics"
);
Expect(
    PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(
        detailedTraceEnabled: true,
        failureOrRecovery: false
    ),
    "explicit opt-in should enable full success diagnostics"
);
Expect(
    PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(
        detailedTraceEnabled: false,
        failureOrRecovery: true
    ),
    "failure and recovery should retain full diagnostics"
);

var scheduleGate = new PostStartupDiagnosticsScheduleGate();
Expect(scheduleGate.TrySchedule(), "the first post-startup schedule should be accepted");
Expect(!scheduleGate.TrySchedule(), "duplicate post-startup scheduling should be rejected");
Expect(scheduleGate.Stop(), "active post-startup diagnostics should stop during cleanup");
Expect(scheduleGate.IsStopped, "post-startup cleanup should latch the stopped state");
Expect(!scheduleGate.Stop(), "duplicate post-startup cleanup should be harmless");
Expect(!scheduleGate.TrySchedule(), "stopped post-startup diagnostics must not restart");

foreach (var rejected in new[]
{
    AndroidMainMenuPreparationResult.TimedOut(),
    AndroidMainMenuPreparationResult.Aborted(),
    AndroidMainMenuPreparationResult.Failed("probe"),
})
{
    Expect(
        PostStartupDiagnosticsPolicy.ShouldWriteFullDiagnostics(
            detailedTraceEnabled: false,
            failureOrRecovery: !rejected.CanExposeMainMenu
        ),
        $"{rejected.Outcome} rendered-frame rejection should retain full diagnostics"
    );
}

var cache = new AndroidAtlasFallbackResolutionCache();
long cacheGeneration = cache.CaptureGeneration();
Expect(
    cache.TryStoreHit("atlas/hit", "texture/hit", cacheGeneration),
    "cached hit should publish"
);
Expect(cache.TryGet("atlas/hit", cacheGeneration, out var hit), "cached hit should be present");
Expect(hit.Found && hit.Fallback == "texture/hit", "cached hit should retain its fallback");
Expect(cache.TryStoreMiss("atlas/miss", cacheGeneration), "cached miss should publish");
Expect(cache.TryGet("atlas/miss", cacheGeneration, out var miss), "cached miss should be present");
Expect(!miss.Found && miss.Fallback.Length == 0, "cached miss should remain a miss");
Expect(cache.Count == 2, "positive and negative cache entries should both count");
Expect(
    cache.TryRemove("atlas/hit", "texture/hit", cacheGeneration),
    "failed fallback loads should invalidate their exact cache entry"
);
Expect(!cache.TryGet("atlas/hit", cacheGeneration, out _), "removed cache entries should not resolve");
var cacheInvalidation = cache.Invalidate();
Expect(
    cacheInvalidation.RemovedCount == 1 && cache.Count == 0,
    "full invalidation should report and clear entries"
);
Expect(
    !cache.TryStoreMiss("atlas/stale", cacheGeneration),
    "old-generation cache publications should be rejected"
);

var resources = AndroidMainMenuWorkingSet.Resources;
Expect(resources.Length > 0, "main-menu working set should not be empty");
Expect(
    resources.Length <= AndroidMainMenuWorkingSet.MaximumResourceCount,
    "main-menu working set should remain bounded"
);
Expect(
    resources.Select(resource => resource.LogicalPath).Distinct(StringComparer.Ordinal).Count()
        == resources.Length,
    "main-menu working set paths should be unique"
);
Expect(
    resources.All(resource => AndroidMainMenuWorkingSetPlan.IsLogicalResourcePath(resource.LogicalPath)),
    "main-menu resources should use logical virtual paths"
);
Expect(
    resources.All(resource => !resource.LogicalPath.Contains(".godot/imported", StringComparison.OrdinalIgnoreCase)),
    "main-menu resources must not name Godot import outputs"
);
Expect(
    resources.All(resource => !System.Text.RegularExpressions.Regex.IsMatch(resource.LogicalPath, "[a-f0-9]{32}")),
    "main-menu resources must not contain import hashes"
);
Expect(
    resources.Any(resource => resource.Kind == AndroidMainMenuResourceKind.Texture),
    "main-menu working set should include observed textures"
);
Expect(
    resources.Any(resource => resource.Kind == AndroidMainMenuResourceKind.Shader),
    "main-menu working set should include observed shaders"
);

var publicIdentity = AndroidMainMenuWorkingSetIdentity.Create(
    "public",
    new string('a', 64),
    "vanilla"
);
Expect(publicIdentity.Branch == "public", "public identity should retain its branch");
Expect(publicIdentity.PckIdentity == new string('a', 12), "PCK identity should be safely shortened");
Expect(publicIdentity.ModMode == "vanilla", "vanilla identity should retain its mode");

var betaIdentity = AndroidMainMenuWorkingSetIdentity.Create(
    "public-beta",
    new string('b', 64),
    "modded"
);
Expect(betaIdentity.Branch == "public-beta", "public-beta identity should retain its branch");
Expect(betaIdentity.PckIdentity == new string('b', 12), "game updates should change PCK identity");
Expect(betaIdentity.ModMode == "modded", "modded identity should retain its mode");

var unsafeIdentity = AndroidMainMenuWorkingSetIdentity.Create(
    "public\nprivate/path",
    "not-a-hash",
    "modded\rpath"
);
Expect(unsafeIdentity.Branch == "<unknown>", "unsafe branch diagnostics should be suppressed");
Expect(unsafeIdentity.PckIdentity == "<unknown>", "invalid PCK diagnostics should be suppressed");
Expect(unsafeIdentity.ModMode == "<unknown>", "unsafe mod diagnostics should be suppressed");

var publicPlan = AndroidMainMenuWorkingSetPlan.Create(resources, _ => true);
Expect(publicPlan.Resources.Count == resources.Length, "public should resolve every available logical resource");
Expect(publicPlan.MissingLogicalPaths.Count == 0, "public should not report available resources missing");

var betaPlan = AndroidMainMenuWorkingSetPlan.Create(resources, _ => true);
Expect(
    betaPlan.Resources.Select(resource => resource.LogicalPath).SequenceEqual(
        publicPlan.Resources.Select(resource => resource.LogicalPath),
        StringComparer.Ordinal
    ),
    "public-beta should resolve the same logical working set without branch hashes"
);

var removedByUpdate = resources[0].LogicalPath;
var updatedPlan = AndroidMainMenuWorkingSetPlan.Create(
    resources,
    path => !string.Equals(path, removedByUpdate, StringComparison.Ordinal)
);
Expect(updatedPlan.Resources.Count == resources.Length - 1, "a game update may remove a logical resource safely");
Expect(
    updatedPlan.MissingLogicalPaths.SequenceEqual(new[] { removedByUpdate }, StringComparer.Ordinal),
    "game-update misses should report the logical resource without stale fallback"
);

var modRequests = new List<string>();
var moddedPlan = AndroidMainMenuWorkingSetPlan.Create(
    resources,
    path =>
    {
        modRequests.Add(path);
        return true;
    }
);
Expect(moddedPlan.Resources.Count == resources.Length, "modded launch should retain the working set");
Expect(
    modRequests.SequenceEqual(resources.Select(resource => resource.LogicalPath), StringComparer.Ordinal),
    "modded resolution must query unchanged logical paths through the active virtual filesystem"
);

var duplicateAndInvalid = new[]
{
    new AndroidMainMenuResource("res://images/ui/example.png", AndroidMainMenuResourceKind.Texture),
    new AndroidMainMenuResource("res://images/ui/example.png", AndroidMainMenuResourceKind.Texture),
    new AndroidMainMenuResource("res://.godot/imported/example.png-deadbeef.ctex", AndroidMainMenuResourceKind.Texture),
    new AndroidMainMenuResource("res://images/../private.png", AndroidMainMenuResourceKind.Texture),
};
var filteredPlan = AndroidMainMenuWorkingSetPlan.Create(duplicateAndInvalid, _ => true);
Expect(filteredPlan.Resources.Count == 1, "only one valid logical resource should remain");
Expect(filteredPlan.DuplicateCount == 1, "duplicate logical resources should be counted");
Expect(filteredPlan.RejectedCount == 2, "internal and traversal paths should be rejected");

var resolved = AndroidMainMenuResourceResolution.Create(
    "res://images/ui/example.png",
    "res://images/ui/example.png"
);
Expect(resolved.LogicalPath == resolved.EffectivePath, "normal virtual resolution should retain logical identity");
var fallbackResolution = AndroidMainMenuResourceResolution.Create(
    "res://images/ui/example.png",
    string.Empty
);
Expect(fallbackResolution.EffectivePath == fallbackResolution.LogicalPath, "empty resource paths should fall back safely");
var privateResolution = AndroidMainMenuResourceResolution.Create(
    "res://images/ui/example.png",
    "/storage/emulated/0/private/example.png"
);
Expect(privateResolution.EffectivePath == "<non-virtual>", "diagnostics must suppress filesystem paths");

var stable = new MainMenuFrameStabilityTracker(
    requiredStableFrames: 3,
    stableFrameThresholdMs: 40,
    minimumObservationMs: 100,
    maximumObservationMs: 500
);
Expect(!stable.ObserveRenderedFrame(20), "one rendered frame cannot prove stability");
Expect(!stable.ObserveRenderedFrame(60), "stability should wait for enough samples");
Expect(!stable.ObserveRenderedFrame(90), "stability should respect the minimum observation period");
Expect(stable.ObserveRenderedFrame(120), "stable rendered frames after the minimum should hand off");
Expect(stable.Outcome == MainMenuFrameStabilityOutcome.Stable, "stable completion should latch");
Expect(stable.FramePostDrawSamples == 4, "rendered-frame samples should be counted");
Expect(stable.SlowFrames == 0, "stable samples should not record slow frames");
Expect(stable.MaximumFrameIntervalMs == 40, "maximum rendered-frame interval should be retained");
Expect(stable.AverageFrameIntervalMs == 30, "average rendered-frame interval should be retained");
Expect(!stable.ObserveRenderedFrame(140), "duplicate rendered frames must not complete twice");
Expect(!stable.TryTimeout(500), "a stable result must not be overwritten by timeout");

var reset = new MainMenuFrameStabilityTracker(2, 40, 0, 200);
Expect(!reset.ObserveRenderedFrame(20), "one stable rendered interval should not complete");
Expect(!reset.ObserveRenderedFrame(80), "a slow rendered frame should reset stability");
Expect(reset.SlowFrames == 1, "a slow frame should be counted");
Expect(!reset.ObserveRenderedFrame(100), "stability should rebuild after a reset");
Expect(reset.ObserveRenderedFrame(120), "two new rendered intervals should complete after reset");

var initialStall = new MainMenuFrameStabilityTracker(1, 40, 0, 200);
Expect(!initialStall.ObserveRenderedFrame(100), "the initial FramePostDraw stall must be measured");
Expect(initialStall.SlowFrames == 1, "the initial stall should count as a slow rendered frame");
Expect(initialStall.MaximumFrameIntervalMs == 100, "the initial stall duration should be retained");

var timedOut = new MainMenuFrameStabilityTracker(2, 40, 0, 200);
Expect(!timedOut.ObserveRenderedFrame(100), "an unstable rendered frame should not hand off");
Expect(!timedOut.TryTimeout(199), "timeout should not latch before the maximum period");
Expect(timedOut.TryTimeout(200), "the maximum observation period should latch timeout");
Expect(timedOut.Outcome == MainMenuFrameStabilityOutcome.TimedOut, "timeout should remain classified");
Expect(!timedOut.TryTimeout(250), "timeout must not complete twice");
Expect(!timedOut.ObserveRenderedFrame(210), "rendered frames after timeout must be ignored");
Expect(!timedOut.TryAbort(), "timeout must not be overwritten by lifecycle teardown");

var aborted = new MainMenuFrameStabilityTracker(2, 40, 0, 200);
Expect(!aborted.ObserveRenderedFrame(20), "one rendered frame should leave the gate pending");
Expect(aborted.TryAbort(), "lifecycle teardown should latch an aborted gate");
Expect(aborted.Outcome == MainMenuFrameStabilityOutcome.Aborted, "abort should remain classified");
Expect(!aborted.TryAbort(), "lifecycle teardown must not complete twice");
Expect(!aborted.ObserveRenderedFrame(40), "rendered frames after teardown must be ignored");
Expect(!aborted.TryTimeout(200), "teardown must not be overwritten by timeout");

Expect(
    AndroidMainMenuPreparationResult.NotRequired().CanExposeMainMenu,
    "non-Android preparation should admit handoff"
);
Expect(
    AndroidMainMenuPreparationResult.Stable().CanExposeMainMenu,
    "stable rendered frames should admit handoff"
);
Expect(
    !AndroidMainMenuPreparationResult.TimedOut().CanExposeMainMenu,
    "rendered-frame timeout should block handoff"
);
Expect(
    !AndroidMainMenuPreparationResult.Aborted().CanExposeMainMenu,
    "lifecycle teardown should block handoff"
);
Expect(
    !AndroidMainMenuPreparationResult.Failed("probe").CanExposeMainMenu,
    "preparation failure should block handoff"
);

long nowMs = 1_000;
var parentDeadline = LauncherMonotonicDeadline.ForTest(
    TimeSpan.FromMilliseconds(100),
    () => nowMs
);
nowMs += 25;
var childDeadline = parentDeadline.CreateChild(TimeSpan.FromMilliseconds(500));
Expect(childDeadline.RemainingDelayMilliseconds == 75, "child deadline must not outlive parent");
nowMs += 75;
Expect(parentDeadline.IsExpired, "parent deadline should expire at its hard bound");
Expect(childDeadline.IsExpired, "child deadline should expire with its parent");

if (failures.Count > 0)
{
    Console.Error.WriteLine("Main-menu performance policy probe failed:");
    foreach (var failure in failures)
        Console.Error.WriteLine($"- {failure}");
    return 1;
}

Console.WriteLine(
    "Main-menu performance policy probe passed "
    + "(diagnostic gating and cleanup, atlas cache invalidation, bounded working set, "
    + "version-safe logical resources, mod-safe virtual resolution, "
    + "rendered-frame stability, latched outcomes, hard deadlines)."
);
return 0;

void Expect(bool condition, string message)
{
    if (!condition)
        failures.Add(message);
}
