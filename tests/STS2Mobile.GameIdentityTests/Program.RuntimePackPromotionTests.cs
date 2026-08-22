using System;
using System.IO;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void RuntimePackPromotionSucceeds()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(fixture, identity);
        True(generation.Succeeded, generation.Problem);
        var cache = new RecordingCachePreparer();

        var result = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            cache
        );

        True(result.Succeeded, $"Validated candidate promotion must succeed: {result.Problem}");
        True(result.Promoted, "The first completed candidate must be promoted.");
        Equal(1, cache.CallCount, "Active-cache preparation must run once after pack promotion.");
        True(cache.FinalPackWasValid, "Active-cache preparation must observe the promoted exact-identity pack.");
        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch);
        True(Directory.Exists(finalDirectory), "The complete runtime-pack directory must be installed at the final path.");
        True(!Directory.Exists(generation.Candidate.StagingDirectory), "Successful promotion must consume candidate staging.");
        True(
            LauncherRuntimeSlotEvidence.IsAuthorized(
                fixture.DataDir,
                identity,
                result.RuntimeSlot.RuntimePack.PackId,
                out var problem
            ),
            $"current_runtime_slot.json must authorize the exact promoted identity: {problem}"
        );
    }

    private static void RuntimePackPromotionFailsBeforeMutation()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(fixture, identity);
        var cache = new RecordingCachePreparer();

        var result = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            cache,
            new RuntimePackLaunchPreparationHooks
            {
                Promotion = new RuntimePackPromotionHooks
                {
                    BeforePromotion = () => throw new SimulatedPromotionException("before-promotion"),
                },
            }
        );

        True(!result.Succeeded, "Failure before promotion must block launch.");
        True(Directory.Exists(generation.Candidate.StagingDirectory), "Failure before mutation must preserve candidate staging.");
        True(!Directory.Exists(GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch)), "Failure before mutation must not create a final pack.");
        Equal(0, cache.CallCount, "Active-cache preparation must not run before pack promotion.");
        True(!LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "Failure must leave launch unauthorized.");
    }

    private static void RuntimePackPromotionDirectoryReplacementRollback()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch);
        Directory.CreateDirectory(finalDirectory);
        var oldSentinel = Path.Combine(finalDirectory, "old-pack.sentinel");
        File.WriteAllText(oldSentinel, "old-pack");
        var generation = GenerateRuntimePackCandidate(fixture, identity);

        var result = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            new RecordingCachePreparer(),
            new RuntimePackLaunchPreparationHooks
            {
                Promotion = new RuntimePackPromotionHooks
                {
                    AfterExistingPackBackedUp = () => throw new SimulatedPromotionException("directory-replacement"),
                },
            }
        );

        True(!result.Succeeded, "A failure between directory renames must block launch.");
        Equal("old-pack", File.ReadAllText(oldSentinel), "The previous final pack must be restored.");
        True(Directory.Exists(generation.Candidate.StagingDirectory), "The candidate must remain retryable when its move never occurred.");
        True(!LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "Directory replacement failure must leave launch unauthorized.");
    }

    private static void RuntimePackPromotionActiveCacheFailure()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(fixture, identity);
        var activePath = Path.Combine(fixture.DataDir, ".godot", "mono", "publish", "arm64", "sts2.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(activePath)!);
        File.WriteAllText(activePath, "previous-active-cache");

        var result = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            new RecordingCachePreparer(fail: true)
        );

        True(!result.Succeeded, "Active-cache replacement failure must block launch.");
        Equal("previous-active-cache", File.ReadAllText(activePath), "A failed cache preparation must preserve the previous cache.");
        True(!LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "Active-cache failure must leave launch unauthorized.");
    }

    private static void RuntimePackPromotionRecoversInterruptedReplacement()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch);
        Directory.CreateDirectory(finalDirectory);
        File.WriteAllText(Path.Combine(finalDirectory, "old-pack.sentinel"), "old-pack");
        var generation = GenerateRuntimePackCandidate(fixture, identity);
        var backupDirectory = finalDirectory
            + $".backup.{generation.Candidate.InstallTransactionId:N}";

        // Exact on-disk state left by process death after both atomic renames but
        // before promoted read-back validation and backup cleanup.
        Directory.Move(finalDirectory, backupDirectory);
        Directory.Move(generation.Candidate.StagingDirectory, finalDirectory);

        var result = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            new RecordingCachePreparer()
        );

        True(result.Succeeded, $"Restart must adopt only the fully validated promoted directory: {result.Problem}");
        True(!result.Promoted, "Recovery must recognize that the candidate rename already completed.");
        True(!Directory.Exists(backupDirectory), "The obsolete backup should be cleaned after final validation.");
        True(LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "Recovered promotion must authorize launch only at the end.");
    }

    private static void RuntimePackPromotionFailureBeforeAuthorizationMarker()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(fixture, identity);
        var cache = new RecordingCachePreparer();

        var result = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            cache,
            new RuntimePackLaunchPreparationHooks
            {
                BeforeAuthorizationMarker = () => throw new SimulatedPromotionException("before-current-runtime-slot"),
            }
        );

        True(!result.Succeeded, "Failure before current_runtime_slot.json must block launch.");
        Equal(1, cache.CallCount, "This cut point must occur after active-cache preparation.");
        True(!LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "The final authorization marker must remain absent.");
    }

    private static void RuntimePackPromotionRejectsStalePack()
    {
        using var fixture = CreateRuntimePackFixture();
        var oldIdentity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(fixture, oldIdentity);
        True(generation.Succeeded, generation.Problem);
        var stalePackFixture = Path.Combine(fixture.DataDir, "stale-pack-test-fixture");
        Directory.Move(generation.Candidate.StagingDirectory, stalePackFixture);

        GameIdentity currentIdentity;
        using (var update = BranchInstallUpdate.StartOrResume(fixture.DataDir, fixture.Branch, LifecycleDepots(1002)))
        {
            ReplaceWithGeneration(fixture, 1002, 0x73, "source-generation-two");
            currentIdentity = update.CompleteInstalledFiles("android-pck-v1").GameIdentity;
        }
        Directory.Move(stalePackFixture, GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch));

        var result = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            currentIdentity,
            candidate: null,
            new RecordingCachePreparer()
        );

        True(!result.Succeeded, "A pack generated for N must not be selected for N+1.");
        Contains(result.Problem, "GameIdentity", "Stale-pack rejection should identify the identity mismatch.");
        True(!LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "A stale pack must not authorize launch.");
    }

    private static void RuntimePackPromotionSecondStartupIsIdempotent()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(fixture, identity);
        var cache = new RecordingCachePreparer();
        var first = RuntimePackLaunchLifecycle.Complete(fixture.DataDir, identity, generation.Candidate, cache);
        True(first.Succeeded, first.Problem);

        var second = RuntimePackLaunchLifecycle.Complete(fixture.DataDir, identity, candidate: null, cache);

        True(second.Succeeded, $"A second startup must reuse the validated pack: {second.Problem}");
        True(!second.Promoted, "The second startup must not replace an already-valid final pack.");
        Equal(first.RuntimeSlot.RuntimePack.PackId, second.RuntimeSlot.RuntimePack.PackId, "The pack ID must remain stable.");
        Equal(2, cache.CallCount, "Each launch boundary reconfirms cache readiness; native may satisfy the second as a hit.");
        True(LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "Second startup must finish authorized.");
    }

    private static void RuntimePackPromotionRejectsBranchIdentityChange()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(fixture, identity);

        var result = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            new RecordingCachePreparer(),
            new RuntimePackLaunchPreparationHooks
            {
                Promotion = new RuntimePackPromotionHooks
                {
                    BeforePromotion = () =>
                    {
                        using var update = BranchInstallUpdate.StartOrResume(fixture.DataDir, fixture.Branch, LifecycleDepots(1002));
                        ReplaceWithGeneration(fixture, 1002, 0x74, "source-generation-two");
                        update.CompleteInstalledFiles("android-pck-v1");
                    },
                },
            }
        );

        True(!result.Succeeded, "A branch identity change during promotion must reject the candidate.");
        True(!LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "An identity race must leave launch unauthorized.");
        True(!Directory.Exists(GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch)), "The stale candidate must not become final.");
    }

    private sealed class RecordingCachePreparer : IRuntimeAssemblyCachePreparer
    {
        private readonly bool _fail;

        internal RecordingCachePreparer(bool fail = false) => _fail = fail;

        internal int CallCount { get; private set; }
        internal bool FinalPackWasValid { get; private set; }

        public RuntimeAssemblyCachePreparationResult Prepare(
            string dataDir,
            GameIdentity gameIdentity,
            RuntimePackManifest manifest
        )
        {
            CallCount++;
            FinalPackWasValid = RuntimePackCandidateValidator.Validate(
                GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, gameIdentity.Branch),
                gameIdentity,
                PatchCompatibilityValidator.PatchSetVersion,
                PatchCompatibilityValidator.ValidationMode,
                PatchCompatibilityValidator.ValidationSurfaceVersion
            ).Usable;
            return _fail
                ? RuntimeAssemblyCachePreparationResult.Rejected("simulated active-cache replacement failure")
                : RuntimeAssemblyCachePreparationResult.Success();
        }
    }

    private sealed class SimulatedPromotionException : IOException
    {
        internal SimulatedPromotionException(string message) : base(message)
        {
        }
    }
}
