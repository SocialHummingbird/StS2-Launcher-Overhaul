using STS2Mobile.Patches;

var failures = new List<string>();

var cache = new AndroidAtlasFallbackResolutionCache();
var tracker = new AndroidAtlasResourceChangeTracker(cache);
long generation = cache.CaptureGeneration();
Expect(cache.TryStoreHit("atlas/standard-hit", "texture/original", generation), "initial hit should publish");
Expect(cache.TryStoreMiss("atlas/standard-miss", generation), "initial negative miss should publish");

var standardMod = tracker.RecordResourcePackLoad(
    loadSucceeded: true,
    resourcesMayHaveChanged: true
);
Expect(standardMod.Invalidated, "a successful standard-mod pack should invalidate");
Expect(standardMod.RemovedCount == 2, "standard-mod invalidation should remove hits and misses");
Expect(standardMod.Generation == generation + 1, "standard-mod load should advance generation once");
Expect(cache.Count == 0, "standard-mod invalidation should empty the cache");
Expect(!cache.TryGet("atlas/standard-miss", generation, out _), "old-generation misses must not resolve");

generation = cache.CaptureGeneration();
Expect(cache.TryStoreMiss("atlas/failed-pack", generation), "failed-pack sentinel should publish");
var failedLoad = tracker.RecordResourcePackLoad(
    loadSucceeded: false,
    resourcesMayHaveChanged: true
);
Expect(!failedLoad.Invalidated, "a failed pack load must not invalidate");
Expect(failedLoad.Generation == generation, "a failed pack load must not advance generation");
Expect(cache.TryGet("atlas/failed-pack", generation, out _), "a failed pack load should leave current entries intact");

var noOpLoad = tracker.RecordResourcePackLoad(
    loadSucceeded: true,
    resourcesMayHaveChanged: false
);
Expect(!noOpLoad.Invalidated, "a declared no-op load must not invalidate");
Expect(noOpLoad.Generation == generation, "a declared no-op load must not advance generation");
Expect(cache.TryGet("atlas/failed-pack", generation, out _), "a no-op should leave current entries intact");

var baseLib = tracker.RecordResourcePackLoad(
    loadSucceeded: true,
    resourcesMayHaveChanged: true
);
Expect(baseLib.Invalidated, "a successful BaseLib pack should use the shared invalidation boundary");
Expect(baseLib.RemovedCount == 1, "BaseLib invalidation should remove the retained negative miss");

generation = cache.CaptureGeneration();
Expect(cache.TryStoreHit("atlas/late", "texture/before-late-pack", generation), "late-pack sentinel should publish");
var latePack = tracker.RecordResourcePackLoad(
    loadSucceeded: true,
    resourcesMayHaveChanged: true
);
Expect(latePack.Invalidated, "a future late-loaded pack should invalidate");
Expect(latePack.RemovedCount == 1, "late-loaded pack should remove earlier fallback mappings");

long beforeRepeatedLoad = cache.CaptureGeneration();
var repeatedLoad = tracker.RecordResourcePackLoad(
    loadSucceeded: true,
    resourcesMayHaveChanged: true
);
Expect(repeatedLoad.Invalidated, "each successful potentially-changing mount should invalidate");
Expect(repeatedLoad.RemovedCount == 0, "repeated invalidation should be harmless when the cache is empty");
Expect(repeatedLoad.Generation == beforeRepeatedLoad + 1, "repeated successful mounts should remain monotonic");

var identityCache = new AndroidAtlasFallbackResolutionCache();
var identityTracker = new AndroidAtlasResourceChangeTracker(identityCache);
long identityGeneration = identityCache.CaptureGeneration();
var initialIdentity = identityTracker.ObserveMountedResourceSet("public|aaaaaaaaaaaa|vanilla");
Expect(initialIdentity.Outcome == AndroidAtlasResourceChangeOutcome.BaselineRecorded, "first branch identity should establish a baseline");
Expect(initialIdentity.Generation == identityGeneration, "baseline observation must not advance generation");
var unchangedIdentity = identityTracker.ObserveMountedResourceSet("public|aaaaaaaaaaaa|vanilla");
Expect(unchangedIdentity.Outcome == AndroidAtlasResourceChangeOutcome.Unchanged, "same branch identity should be a no-op");
Expect(unchangedIdentity.Generation == identityGeneration, "same branch identity must not advance generation");
Expect(identityCache.TryStoreMiss("atlas/branch-miss", identityGeneration), "branch negative miss should publish");
var changedIdentity = identityTracker.ObserveMountedResourceSet("public-beta|bbbbbbbbbbbb|modded");
Expect(changedIdentity.Invalidated, "a branch/PCK/mod-mode resource-set change should invalidate");
Expect(changedIdentity.RemovedCount == 1, "branch resource change should remove negative misses");
Expect(changedIdentity.Generation == identityGeneration + 1, "branch resource change should advance generation once");
Expect(identityCache.Count == 0, "branch resource changes should leave no stale fallback entries");

var concurrentCache = new AndroidAtlasFallbackResolutionCache();
var concurrentTracker = new AndroidAtlasResourceChangeTracker(concurrentCache);
var publisherReady = new ManualResetEventSlim(false);
var releasePublisher = new ManualResetEventSlim(false);
var staleMissPublisher = Task.Run(() =>
{
    long capturedGeneration = concurrentCache.CaptureGeneration();
    publisherReady.Set();
    releasePublisher.Wait();
    return concurrentCache.TryStoreMiss("atlas/concurrent-miss", capturedGeneration);
});
publisherReady.Wait();
var concurrentInvalidation = concurrentTracker.RecordResourcePackLoad(
    loadSucceeded: true,
    resourcesMayHaveChanged: true
);
releasePublisher.Set();
Expect(!staleMissPublisher.GetAwaiter().GetResult(), "an in-flight old-generation miss must not publish after invalidation");
Expect(concurrentInvalidation.Invalidated, "concurrent successful load should invalidate");
Expect(concurrentCache.Count == 0, "concurrent invalidation should not retain a stale negative miss");

long staleHitGeneration = concurrentCache.CaptureGeneration();
concurrentTracker.RecordResourcePackLoad(loadSucceeded: true, resourcesMayHaveChanged: true);
Expect(
    !concurrentCache.TryStoreHit("atlas/concurrent-hit", "texture/stale", staleHitGeneration),
    "an old-generation positive fallback must not publish after invalidation"
);

if (failures.Count > 0)
{
    Console.Error.WriteLine("Atlas resource-pack policy probe failed:");
    foreach (var failure in failures)
        Console.Error.WriteLine($"- {failure}");
    return 1;
}

Console.WriteLine(
    "Atlas resource-pack policy probe passed "
    + "(standard mods, BaseLib, late packs, branch changes, "
    + "failed/no-op loads, repeated invalidation, negative misses, concurrent publication)."
);
return 0;

void Expect(bool condition, string message)
{
    if (!condition)
        failures.Add(message);
}
