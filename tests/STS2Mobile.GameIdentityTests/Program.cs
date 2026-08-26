using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static int Main()
    {
        using var markerFiles = TestMarkerFiles.PreserveCurrentDirectory();

        var tests = new (string Name, Action Run)[]
        {
            ("stable identity and safe PCK cache hit", StableIdentityAndPckCacheHit),
            ("changed PCK at same path", ChangedPckAtSamePath),
            ("changed DLL at same path", ChangedDllAtSamePath),
            ("changed DLL with same size and timestamp", ChangedDllWithSameSizeAndTimestamp),
            ("readiness cache uses authoritative identity", ReadinessCacheUsesAuthoritativeIdentity),
            ("legacy PCK cache and identity evidence rejected", LegacyCacheAndEvidenceRejected),
            ("branch normalization", BranchNormalization),
            ("missing file fails closed", MissingFileFailsClosed),
            ("incomplete install marker fails closed", IncompleteInstallMarkerFailsClosed),
            ("partially replaced install fails closed", PartiallyReplacedInstallFailsClosed),
            ("file changing during hash fails closed", FileChangingDuringHashFailsClosed),
            ("unreadable file fails closed", UnreadableFileFailsClosed),
            ("PCK cache is persisted atomically", PckCacheIsPersistedAtomically),
            ("stale validation report cannot override current files", StaleValidationReportCannotOverrideCurrentFiles),
            ("stale runtime pack cannot override changed files", StaleRuntimePackCannotOverrideChangedFiles),
            ("stale runtime slot marker cannot make slot playable", StaleRuntimeSlotMarkerCannotMakeSlotPlayable),
            ("matching derived evidence remains playable", MatchingDerivedEvidenceRemainsPlayable),
            ("incompatible derived evidence fails closed", IncompatibleDerivedEvidenceFailsClosed),
            ("branch state updating to ready transition", BranchStateUpdatingToReadyTransition),
            ("branch state interrupted atomic write", BranchStateInterruptedAtomicWrite),
            ("branch state interrupted ready revocation", BranchStateInterruptedReadyRevocation),
            ("branch state corrupt or truncated", BranchStateCorruptOrTruncated),
            ("branch state legacy evidence", BranchStateLegacyEvidence),
            ("branch state identity generation mismatch", BranchStateIdentityGenerationMismatch),
            ("branch state slots are independent", BranchStateSlotsAreIndependent),
            ("branch state repeated and concurrent transitions", BranchStateRepeatedAndConcurrentTransitions),
            ("branch lifecycle successful N to N+1", BranchLifecycleSuccessfulReplacement),
            ("branch lifecycle restart before first replacement", BranchLifecycleRestartBeforeFirstReplacement),
            ("branch lifecycle restart after one replacement", BranchLifecycleRestartAfterOneReplacement),
            ("branch lifecycle restart before ready publication", BranchLifecycleRestartBeforeReadyPublication),
            ("branch lifecycle restart after completed files", BranchLifecycleRestartAfterCompletedFiles),
            ("branch lifecycle restart after install ready", BranchLifecycleRestartAfterInstallReady),
            ("branch lifecycle update preserves other branch", BranchLifecyclePreservesOtherBranch),
            ("runtime pack candidate generation", RuntimePackCandidateGeneration),
            ("runtime pack candidate rejects stale identity", RuntimePackCandidateRejectsStaleIdentity),
            ("runtime pack candidate rejects changing source", RuntimePackCandidateRejectsChangingSource),
            ("runtime pack candidate rejects declared source hashes", RuntimePackCandidateRejectsDeclaredSourceHashes),
            ("runtime pack candidate rejects patched assembly hash", RuntimePackCandidateRejectsPatchedAssemblyHash),
            ("runtime pack candidate rejects missing or corrupt report", RuntimePackCandidateRejectsMissingOrCorruptReport),
            ("runtime pack candidate rejects interrupted staging", RuntimePackCandidateRejectsInterruptedStaging),
            ("runtime pack candidate failure preserves active cache", RuntimePackCandidateFailurePreservesActiveCache),
            ("runtime pack promotion succeeds and authorizes launch", RuntimePackPromotionSucceeds),
            ("runtime pack promotion failure before mutation", RuntimePackPromotionFailsBeforeMutation),
            ("runtime pack promotion directory replacement rollback", RuntimePackPromotionDirectoryReplacementRollback),
            ("runtime pack promotion recovers interrupted replacement", RuntimePackPromotionRecoversInterruptedReplacement),
            ("runtime pack promotion active-cache failure", RuntimePackPromotionActiveCacheFailure),
            ("runtime pack promotion failure before authorization marker", RuntimePackPromotionFailureBeforeAuthorizationMarker),
            ("runtime pack promotion rejects stale pack", RuntimePackPromotionRejectsStalePack),
            ("runtime pack promotion second startup is idempotent", RuntimePackPromotionSecondStartupIsIdempotent),
            ("runtime pack promotion rejects branch identity change", RuntimePackPromotionRejectsBranchIdentityChange),
            ("selected recovery clears beta and preserves public", SelectedRecoveryClearsBetaAndPreservesPublic),
            ("selected recovery clears public and preserves beta", SelectedRecoveryClearsPublicAndPreservesBeta),
            ("selected recovery preserves saves and credentials", SelectedRecoveryPreservesSavesAndCredentials),
            ("selected recovery is idempotent", SelectedRecoveryIsIdempotent),
            ("selected recovery removes corrupt evidence", SelectedRecoveryRemovesCorruptEvidence),
            ("selected recovery completes from updating state", SelectedRecoveryCompletesFromUpdatingState),
            ("selected recovery clears interrupted runtime generation", SelectedRecoveryClearsInterruptedRuntimeGeneration),
            ("selected recovery supports redownload and second launch", SelectedRecoverySupportsRedownloadAndSecondLaunch),
            ("issue 38 complete N to N+1 lifecycle", Issue38CompleteLifecycle),
            ("issue 38 mixed and corrupt packs fail closed", Issue38MixedAndCorruptPacksFailClosed),
            ("installed version readiness accepts current preparation", InstalledVersionReadinessAcceptsCurrentPreparation),
            ("installed version readiness permits safe automatic repair", InstalledVersionReadinessPermitsSafeAutomaticRepair),
            ("installed version readiness rejects unknown preparation", InstalledVersionReadinessRejectsUnknownPreparation),
            ("installed version readiness rejects missing files", InstalledVersionReadinessRejectsMissingFiles),
            ("installed version readiness rejects corrupt state", InstalledVersionReadinessRejectsCorruptState),
            ("installed version readiness rejects changed identity", InstalledVersionReadinessRejectsChangedIdentity),
            ("installed version readiness rejects branch mismatch", InstalledVersionReadinessRejectsBranchMismatch),
            ("installed version readiness rejects provenance mismatch", InstalledVersionReadinessRejectsProvenanceMismatch),
            ("installed version readiness rejects corrupt PCK", InstalledVersionReadinessRejectsCorruptPck),
            ("installed version readiness rejects unrecognized FMOD form", InstalledVersionReadinessRejectsUnrecognizedFmodForm),
            ("installed version readiness rejects missing managed entry", InstalledVersionReadinessRejectsMissingManagedEntry),
            ("installed version readiness inspection is read only", InstalledVersionReadinessInspectionIsReadOnly),
            ("local PCK repair completes with isolated invalidation", LocalPckRepairCompletesWithIsolatedInvalidation),
            ("local PCK repair regenerates runtime and preserves unrelated state", LocalPckRepairRegeneratesRuntimeAndPreservesUnrelatedState),
            ("local PCK repair repeated call is a no-op", LocalPckRepairRepeatedCallIsNoOp),
            ("local PCK repair resumes before PCK mutation", LocalPckRepairResumesBeforePckMutation),
            ("local PCK repair resumes before ready publication", LocalPckRepairResumesBeforeReadyPublication),
            ("local PCK repair rejects unknown and torn entries", LocalPckRepairRejectsUnknownAndTornEntries),
            ("automatic repair routing selects only local repair", AutomaticRepairRoutingSelectsOnlyLocalRepair),
            ("automatic repair model uses existing guard and completion", AutomaticRepairModelUsesExistingGuardAndCompletion),
            ("interrupted repair routes back to automatic repair", InterruptedRepairRoutesBackToAutomaticRepair),
            ("handoff attempt IDs are stable and structured", HandoffAttemptIdsAreStableAndStructured),
            ("main-menu readiness normal", MainMenuReadinessNormal),
            ("main-menu readiness repeated operations", MainMenuReadinessRepeatedOperations),
            ("main-menu readiness ignores stale attempts", MainMenuReadinessIgnoresStaleAttempts),
            ("main-menu readiness resets for new attempt", MainMenuReadinessResetsForNewAttempt),
            ("main-menu readiness producer before consumer", MainMenuReadinessProducerBeforeConsumer),
            ("main-menu readiness consumer before producer", MainMenuReadinessConsumerBeforeProducer),
            ("handoff active main-menu producer is idempotent", HandoffActiveMainMenuProducerIsIdempotent),
            ("handoff integration readiness before focus", HandoffIntegrationReadinessBeforeFocus),
            ("handoff integration focus before readiness", HandoffIntegrationFocusBeforeReadiness),
            ("handoff integration ignores stale events", HandoffIntegrationIgnoresStaleEvents),
            ("handoff lifecycle background resume before readiness", HandoffLifecycleBackgroundResumeBeforeReadiness),
            ("handoff lifecycle background resume after readiness", HandoffLifecycleBackgroundResumeAfterReadiness),
            ("handoff lifecycle lock unlock focus", HandoffLifecycleLockUnlockFocus),
            ("handoff lifecycle focus loss restoration", HandoffLifecycleFocusLossRestoration),
            ("handoff coordinator wakes only for state changes", HandoffCoordinatorWakesOnlyForStateChanges),
            ("RC3 captured passing launch sequence", Rc3CapturedPassingLaunchSequence),
            ("RC3 captured failing launch sequence regression", Rc3CapturedFailingLaunchSequenceNowCompletes),
            ("handoff integration subscription before readiness", HandoffIntegrationSubscriptionBeforeReadiness),
            ("stale mainMenu true cannot complete active handoff", StaleMainMenuTrueCannotCompleteActiveHandoff),
            ("handoff state owner normal events", HandoffStateOwnerNormalEvents),
            ("handoff state owner repeated events", HandoffStateOwnerRepeatedEvents),
            ("handoff state owner reordered events", HandoffStateOwnerReorderedEvents),
            ("handoff state owner stale events", HandoffStateOwnerStaleEvents),
            ("handoff state owner cancelled events", HandoffStateOwnerCancelledEvents),
            ("handoff state owner failed events", HandoffStateOwnerFailedEvents),
            ("handoff owner dismisses overlay exactly once", HandoffOwnerDismissesOverlayExactlyOnce),
            ("handoff recovery launch uses current attempt", HandoffRecoveryLaunchUsesCurrentAttempt),
            ("Android handoff visibility requires resumed focus", AndroidHandoffVisibilityRequiresResumedFocus),
            ("handoff process restoration retains attempt ID", HandoffProcessRestorationRetainsAttemptId),
            ("handoff Android lifecycle permutations", HandoffAndroidLifecyclePermutations),
            ("handoff failure and cancellation restore launcher", HandoffFailureAndCancellationRestoreUsableLauncher),
            ("handoff activity recreation preserves evidence", HandoffActivityRecreationPreservesEvidence),
            ("handoff timeout excludes paused time", HandoffTimeoutExcludesPausedTime),
            ("handoff paused wait resumes deterministically", HandoffPausedWaitResumesDeterministically),
            ("handoff offline cold and warm launches", HandoffOfflineColdAndWarmLaunches),
            ("handoff two consecutive process launches", HandoffTwoConsecutiveProcessLaunches),
            ("handoff timeout recovery rejects late callbacks", HandoffTimeoutRecoveryRejectsLateCallbacks),
            ("handoff simulates 256 reordered event sequences", HandoffSimulatesReorderedEventSequences),
        };

        var failures = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"FAIL {test.Name}: {ex}");
            }
        }

        Console.WriteLine($"GameIdentity tests: {tests.Length - failures} passed, {failures} failed");
        return failures == 0 ? 0 : 1;
    }

    private static void StableIdentityAndPckCacheHit()
    {
        using var fixture = GameInstallFixture.Create();
        var first = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        var hasher = new CountingHasher();
        var second = new GameIdentityReader(hasher).Read(fixture.DataDir, fixture.Branch);

        Equal(first, second, "Unchanged files must produce the same complete identity.");
        Equal(0, hasher.CountFor(fixture.PckPath), "A complete generation-bound PCK cache should be reused.");
        Equal(1, hasher.CountFor(fixture.SourceAssemblyPath), "sts2.dll must always be hashed directly.");
    }

    private static void ChangedPckAtSamePath()
    {
        using var fixture = GameInstallFixture.Create();
        var before = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        fixture.WritePck(0x42);
        fixture.CompleteGeneration(1002);
        var hasher = new CountingHasher();
        var after = new GameIdentityReader(hasher).Read(fixture.DataDir, fixture.Branch);

        NotEqual(before.PckSha256, after.PckSha256, "A changed PCK must change current identity.");
        NotEqual(before.InstallGeneration, after.InstallGeneration, "A completed update must change install generation.");
        Equal(1, hasher.CountFor(fixture.PckPath), "The old generation's PCK cache must not be reused.");
    }

    private static void ChangedDllAtSamePath()
    {
        using var fixture = GameInstallFixture.Create();
        var before = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        fixture.WriteSourceAssembly("source-generation-two");
        fixture.CompleteGeneration(1002);
        var after = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);

        NotEqual(before.SourceAssemblySha256, after.SourceAssemblySha256, "A changed sts2.dll must change current identity.");
    }

    private static void ChangedDllWithSameSizeAndTimestamp()
    {
        using var fixture = GameInstallFixture.Create(sourceText: "AAAAAAAAAAAAAAAAAAAA");
        var before = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        var originalMtime = File.GetLastWriteTimeUtc(fixture.SourceAssemblyPath);
        fixture.WriteSourceAssembly("BBBBBBBBBBBBBBBBBBBB");
        File.SetLastWriteTimeUtc(fixture.SourceAssemblyPath, originalMtime);
        fixture.CompleteGeneration(1002);
        var after = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);

        NotEqual(before.SourceAssemblySha256, after.SourceAssemblySha256, "DLL hashing must not trust size or timestamp.");
    }

    private static void ReadinessCacheUsesAuthoritativeIdentity()
    {
        using var fixture = GameInstallFixture.Create(sourceText: "AAAAAAAAAAAAAAAAAAAA");
        var cached = LauncherLaunchReadinessCacheIdentities.Capture(fixture.DataDir, fixture.Branch);
        var originalMtime = File.GetLastWriteTimeUtc(fixture.SourceAssemblyPath);
        fixture.WriteSourceAssembly("BBBBBBBBBBBBBBBBBBBB");
        File.SetLastWriteTimeUtc(fixture.SourceAssemblyPath, originalMtime);

        var matches = cached.MatchesCurrent(fixture.DataDir, fixture.Branch, out var mismatchReason);

        True(!matches, "Readiness cache must not treat a same-size/same-timestamp DLL replacement as unchanged.");
        Contains(mismatchReason, "authoritative game identity", "The cache miss should identify the authoritative identity change.");
    }

    private static void LegacyCacheAndEvidenceRejected()
    {
        using var fixture = GameInstallFixture.Create();
        var fakeHash = new string('0', 64);
        Directory.CreateDirectory(Path.GetDirectoryName(GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch))!);
        File.WriteAllText(
            GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch),
            JsonSerializer.Serialize(new
            {
                schemaVersion = 0,
                branch = fixture.Branch,
                pckPath = fixture.PckPath,
                pckSha256 = fakeHash,
            })
        );
        File.WriteAllText(Path.Combine(fixture.GameDirectory, ".android_patch_validation.json"),
            JsonSerializer.Serialize(new { status = "passed", pckSha256 = fakeHash, sourceAssemblySha256 = fakeHash }));
        var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch);
        Directory.CreateDirectory(runtimePackDirectory);
        File.WriteAllText(Path.Combine(runtimePackDirectory, "compatibility.json"),
            JsonSerializer.Serialize(new { sourcePckSha256 = fakeHash, sourceAssemblySha256 = fakeHash }));
        File.WriteAllText(Path.Combine(fixture.DataDir, "current_runtime_slot.json"),
            JsonSerializer.Serialize(new { pckSha256 = fakeHash, sourceAssemblySha256 = fakeHash }));

        var hasher = new CountingHasher();
        var identity = new GameIdentityReader(hasher).Read(fixture.DataDir, fixture.Branch);

        Equal(1, hasher.CountFor(fixture.PckPath), "A legacy cache missing generation/length/mtime must be rejected.");
        NotEqual(fakeHash, identity.PckSha256, "Legacy evidence must not supply the current PCK identity.");
        NotEqual(fakeHash, identity.SourceAssemblySha256, "Legacy evidence must not supply the current DLL identity.");
    }

    private static void BranchNormalization()
    {
        using var fixture = GameInstallFixture.Create();
        var mixed = GameIdentityReader.ReadInstalled(fixture.DataDir, "  PuBlic-BeTa  ");
        var normalized = GameIdentityReader.ReadInstalled(fixture.DataDir, "public-beta");

        Equal("public-beta", mixed.Branch, "Identity must store the canonical Steam branch.");
        Equal(mixed, normalized, "Branch spelling/case must not create a different identity.");
    }

    private static void MissingFileFailsClosed()
    {
        using var fixture = GameInstallFixture.Create();
        File.Delete(fixture.SourceAssemblyPath);

        var exception = Throws<GameIdentityException>(
            () => GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch)
        );
        Equal(GameIdentityFailureKind.MissingFile, exception.Kind, "Missing source file must fail closed.");
        Contains(exception.Message, "sts2.dll", "Failure should identify the missing file.");

        using var pckFixture = GameInstallFixture.Create();
        File.Delete(pckFixture.PckPath);
        var pckException = Throws<GameIdentityException>(
            () => GameIdentityReader.ReadInstalled(pckFixture.DataDir, pckFixture.Branch)
        );
        Equal(GameIdentityFailureKind.MissingFile, pckException.Kind, "Missing PCK must fail closed.");
        Contains(pckException.Message, "PCK", "Failure should identify the missing PCK.");
    }

    private static void IncompleteInstallMarkerFailsClosed()
    {
        using var fixture = GameInstallFixture.Create();
        File.WriteAllText(
            fixture.BranchMarkerPath,
            $"Branch: {fixture.Branch}\n"
                + "Depot manifest count: 2\n"
                + $"Depot manifest: depot=123 manifest=1001 branch={fixture.Branch} manifestSource=selected\n",
            Encoding.UTF8
        );

        var exception = Throws<GameIdentityException>(
            () => GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch)
        );
        Equal(GameIdentityFailureKind.InvalidInstallGeneration, exception.Kind, "A truncated depot list is not a completed install generation.");
        Contains(exception.Message, "depot provenance", "Failure should identify incomplete depot provenance.");
    }

    private static void PartiallyReplacedInstallFailsClosed()
    {
        using var fixture = GameInstallFixture.Create();
        var markerMtime = DateTime.UtcNow.AddMinutes(-5);
        File.SetLastWriteTimeUtc(fixture.BranchMarkerPath, markerMtime);
        fixture.WritePck(0x51);
        File.SetLastWriteTimeUtc(fixture.PckPath, DateTime.UtcNow);

        var exception = Throws<GameIdentityException>(
            () => GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch)
        );
        Equal(GameIdentityFailureKind.IncompleteInstall, exception.Kind, "Files newer than the completion marker are partial.");
        Contains(exception.Message, "partially replaced", "Failure should explain the partial-install state.");
    }

    private static void FileChangingDuringHashFailsClosed()
    {
        using var fixture = GameInstallFixture.Create();
        var hasher = new MutatingHasher(fixture.SourceAssemblyPath, path => File.AppendAllText(path, "changed"));

        var exception = Throws<GameIdentityException>(
            () => new GameIdentityReader(hasher).Read(fixture.DataDir, fixture.Branch)
        );
        Equal(GameIdentityFailureKind.FileChanged, exception.Kind, "A changing source file must fail closed.");
    }

    private static void UnreadableFileFailsClosed()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using var fixture = GameInstallFixture.Create();
        using var locked = new FileStream(
            fixture.SourceAssemblyPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.None
        );
        var exception = Throws<GameIdentityException>(
            () => GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch)
        );
        Equal(GameIdentityFailureKind.UnreadableFile, exception.Kind, "An unreadable source file must fail closed.");
        Contains(exception.Message, "Cannot read", "Failure should explain that the file could not be read.");
    }

    private static void PckCacheIsPersistedAtomically()
    {
        using var fixture = GameInstallFixture.Create();
        var identity = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        var cachePath = GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch);

        True(File.Exists(cachePath), "A successful identity read should persist the PCK cache.");
        using var document = JsonDocument.Parse(File.ReadAllText(cachePath));
        var root = document.RootElement;
        Equal(1, root.GetProperty("schemaVersion").GetInt32(), "Cache schema must reject legacy layouts.");
        Equal(identity.Branch, root.GetProperty("branch").GetString(), "Cache must bind normalized branch.");
        Equal(identity.InstallGeneration, root.GetProperty("installGeneration").GetString(), "Cache must bind install generation.");
        Equal(GameIdentityPckCache.NormalizePath(fixture.PckPath), root.GetProperty("pckPath").GetString(), "Cache must bind normalized path.");
        Equal(new FileInfo(fixture.PckPath).Length, root.GetProperty("pckLength").GetInt64(), "Cache must bind length.");
        Equal(File.GetLastWriteTimeUtc(fixture.PckPath).Ticks, root.GetProperty("pckLastWriteTimeUtcTicks").GetInt64(), "Cache must bind mtime.");
        Equal(identity.PckSha256, root.GetProperty("pckSha256").GetString(), "Cache must persist the actual PCK hash.");

        var directory = Path.GetDirectoryName(cachePath)!;
        var temporaryFiles = Directory.GetFiles(directory, GameIdentityPckCache.FileName + ".*.tmp");
        Equal(0, temporaryFiles.Length, "Atomic cache persistence must not leave temporary files.");
    }

    private static void StaleValidationReportCannotOverrideCurrentFiles()
    {
        using var fixture = GameInstallFixture.Create();
        var identity = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        File.WriteAllText(
            Path.Combine(fixture.GameDirectory, ".android_patch_validation.json"),
            JsonSerializer.Serialize(new
            {
                status = "passed",
                branch = identity.Branch,
                pckSha256 = identity.PckSha256,
                sourceAssemblySha256 = identity.SourceAssemblySha256,
            })
        );
        fixture.WriteSourceAssembly("source-generation-two");
        fixture.CompleteGeneration(1002);

        var slot = GameRuntimeSlot.Inspect(fixture.DataDir, fixture.Branch);

        NotEqual(identity, slot.GameIdentity, "The report must not override the changed current files.");
        True(!slot.Playable, "A stale game-directory validation report cannot replace a runtime pack.");
        Equal("not installed", slot.RuntimePack.Status, "The stale validation report must not be inspected as pack evidence.");
    }

    private static void StaleRuntimePackCannotOverrideChangedFiles()
    {
        using var fixture = GameInstallFixture.Create();
        fixture.WriteMatchingRuntimePack();
        fixture.WriteSourceAssembly("source-generation-two");
        fixture.CompleteGeneration(1002);

        var slot = GameRuntimeSlot.Inspect(fixture.DataDir, fixture.Branch);

        True(!slot.Playable, "A pack for generation N cannot authorize generation N+1 files.");
        Contains(slot.RuntimePack.Status, "game identity mismatch", "The stale pack should fail complete identity equality.");
    }

    private static void StaleRuntimeSlotMarkerCannotMakeSlotPlayable()
    {
        using var fixture = GameInstallFixture.Create();
        var identity = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        File.WriteAllText(
            Path.Combine(fixture.DataDir, "current_runtime_slot.json"),
            JsonSerializer.Serialize(new
            {
                branch = identity.Branch,
                gameIdentityId = identity.Id,
                installGeneration = identity.InstallGeneration,
                pckSha256 = identity.PckSha256,
                sourceAssemblySha256 = identity.SourceAssemblySha256,
                filesReady = true,
                playable = true,
                runtimeCompatible = true,
                patchCompatible = true,
            })
        );
        fixture.WriteSourceAssembly("source-generation-two");
        fixture.CompleteGeneration(1002);

        var slot = GameRuntimeSlot.Inspect(fixture.DataDir, fixture.Branch);

        NotEqual(identity, slot.GameIdentity, "The runtime-slot marker must not replace the changed current identity.");
        True(!slot.Playable, "A runtime-slot output cannot authorize a slot without matching pack evidence.");
        True(!slot.RuntimePackUsable, "Runtime-slot output must not synthesize a usable runtime pack.");
    }

    private static void MatchingDerivedEvidenceRemainsPlayable()
    {
        using var fixture = GameInstallFixture.Create();
        var identity = fixture.WriteMatchingRuntimePack();

        var slot = GameRuntimeSlot.Inspect(fixture.DataDir, fixture.Branch);

        Equal(identity, slot.GameIdentity, "The slot must retain the identity read from current files.");
        True(slot.RuntimePackUsable, "A pack declaring the complete current identity should be usable.");
        True(slot.PatchCompatible, "Its matching validation report should pass.");
        True(slot.Playable, "Correct files and matching derived evidence should remain playable.");
    }

    private static void IncompatibleDerivedEvidenceFailsClosed()
    {
        using var fixture = GameInstallFixture.Create();
        fixture.WriteMatchingRuntimePack(reportGameIdentityId: new string('0', 64));

        var slot = GameRuntimeSlot.Inspect(fixture.DataDir, fixture.Branch);

        True(!slot.RuntimePackUsable, "A report that disagrees with its manifest must invalidate the pack.");
        True(!slot.Playable, "Incompatible derived evidence must fail closed.");
        Contains(slot.RuntimePack.Status, "report mismatch", "The failure should identify the incompatible report.");
    }

    private static TException Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            return ex;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"{message} Expected={expected}; Actual={actual}.");
    }

    private static void NotEqual<T>(T unexpected, T actual, string message)
    {
        if (EqualityComparer<T>.Default.Equals(unexpected, actual))
            throw new InvalidOperationException($"{message} Unexpected={unexpected}.");
    }

    private static void True(bool value, string message)
    {
        if (!value)
            throw new InvalidOperationException(message);
    }

    private static void Contains(string value, string expected, string message)
    {
        if (value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
            throw new InvalidOperationException($"{message} Value={value}");
    }

    private sealed class TestMarkerFiles : IDisposable
    {
        private readonly Dictionary<string, byte[]?> _originals;

        private TestMarkerFiles(Dictionary<string, byte[]?> originals)
        {
            _originals = originals;
        }

        internal static TestMarkerFiles PreserveCurrentDirectory()
        {
            var originals = new Dictionary<string, byte[]?>(PathComparer());
            foreach (var fileName in new[]
            {
                "last_startup_context.txt",
                "last_startup_timeline.txt",
            })
            {
                var path = Path.GetFullPath(fileName);
                originals[path] = File.Exists(path)
                    ? File.ReadAllBytes(path)
                    : null;
            }
            return new TestMarkerFiles(originals);
        }

        public void Dispose()
        {
            foreach (var pair in _originals)
            {
                try
                {
                    if (pair.Value == null)
                    {
                        if (File.Exists(pair.Key))
                            File.Delete(pair.Key);
                    }
                    else
                    {
                        File.WriteAllBytes(pair.Key, pair.Value);
                    }
                }
                catch
                {
                }
            }
        }
    }

    private sealed class CountingHasher : IGameIdentityFileHasher
    {
        private readonly Dictionary<string, int> _counts = new(PathComparer());

        public string Sha256(string path)
        {
            path = Path.GetFullPath(path);
            _counts[path] = CountFor(path) + 1;
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        internal int CountFor(string path)
            => _counts.TryGetValue(Path.GetFullPath(path), out var count) ? count : 0;
    }

    private sealed class MutatingHasher : IGameIdentityFileHasher
    {
        private readonly string _target;
        private readonly Action<string> _mutation;

        internal MutatingHasher(string target, Action<string> mutation)
        {
            _target = Path.GetFullPath(target);
            _mutation = mutation;
        }

        public string Sha256(string path)
        {
            string hash;
            using (var stream = File.OpenRead(path))
                hash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (PathComparer().Equals(Path.GetFullPath(path), _target))
                _mutation(path);
            return hash;
        }
    }

    private sealed class GameInstallFixture : IDisposable
    {
        internal GameInstallFixture(string dataDir, string branch)
        {
            DataDir = dataDir;
            Branch = branch;
            GameDirectory = SteamGameInstallPaths.GameDirectory(dataDir, branch);
            PckPath = Path.Combine(GameDirectory, "SlayTheSpire2.pck");
            SourceAssemblyPath = Path.Combine(GameDirectory, "data_sts2_windows_x86_64", "sts2.dll");
            BranchMarkerPath = SteamGameInstallPaths.BranchMarkerPath(dataDir, branch);
        }

        internal string DataDir { get; }
        internal string Branch { get; }
        internal string GameDirectory { get; }
        internal string PckPath { get; }
        internal string SourceAssemblyPath { get; }
        internal string BranchMarkerPath { get; }

        internal static GameInstallFixture Create(
            string branch = "public-beta",
            string sourceText = "source-generation-one"
        )
        {
            var dataDir = Path.Combine(
                Path.GetTempPath(),
                "sts2-game-identity-tests",
                Guid.NewGuid().ToString("N")
            );
            var fixture = new GameInstallFixture(dataDir, branch);
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.SourceAssemblyPath)!);
            fixture.WritePck(0x21);
            fixture.WriteSourceAssembly(sourceText);
            fixture.CompleteGeneration(1001);
            return fixture;
        }

        internal void WritePck(byte payload)
        {
            Directory.CreateDirectory(GameDirectory);
            var bytes = new byte[160];
            using (var memory = new MemoryStream(bytes, writable: true))
            using (var writer = new BinaryWriter(memory, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(0x43504447u);
                writer.Write(3u);
                writer.Write(4u);
                writer.Write(5u);
                writer.Write(0u);
                writer.Write(0u);
                writer.Write(0L);
                writer.Write(96L);
                memory.Position = 80;
                writer.Write(payload);
                memory.Position = 96;
                writer.Write(1u);
                memory.Position = 120;
                writer.Write(payload);
            }
            File.WriteAllBytes(PckPath, bytes);
            File.SetLastWriteTimeUtc(PckPath, DateTime.UtcNow.AddMinutes(-2));
        }

        internal void WriteSourceAssembly(string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SourceAssemblyPath)!);
            File.WriteAllText(SourceAssemblyPath, text, Encoding.UTF8);
            File.SetLastWriteTimeUtc(SourceAssemblyPath, DateTime.UtcNow.AddMinutes(-2));
        }

        internal void CompleteGeneration(ulong manifestId)
        {
            Directory.CreateDirectory(GameDirectory);
            File.WriteAllText(
                BranchMarkerPath,
                $"Branch: {Branch}\n"
                    + $"Updated UTC: {DateTime.UtcNow:O}\n"
                    + "Depot manifest count: 1\n"
                    + $"Depot manifest: depot=123 manifest={manifestId} branch={Branch} manifestSource=selected\n",
                Encoding.UTF8
            );
            File.SetLastWriteTimeUtc(BranchMarkerPath, DateTime.UtcNow);
        }

        internal GameIdentity WriteMatchingRuntimePack(
            string? reportGameIdentityId = null
        )
        {
            var identity = GameIdentityReader.ReadInstalled(DataDir, Branch);
            var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(DataDir, Branch);
            Directory.CreateDirectory(runtimePackDirectory);
            var androidAssemblyPath = Path.Combine(runtimePackDirectory, "sts2.dll");
            File.WriteAllText(androidAssemblyPath, "android-runtime-generation-one", Encoding.UTF8);
            var androidAssemblySha256 = HashFile(androidAssemblyPath);
            const string validationSurface = "critical-startup-save-platform-model-v1";
            var supportAssemblies = Array.Empty<string>();
            var supportAssemblySha256 = new Dictionary<string, string>();
            var packId = RuntimePackWriter.RuntimePackId(
                identity,
                PatchCompatibilityValidator.PatchSetVersion,
                validationSurface,
                androidAssemblySha256,
                supportAssemblySha256
            );
            var declaredIdentity = new
            {
                schemaVersion = GameIdentity.SchemaVersion,
                branch = identity.Branch,
                installGeneration = identity.InstallGeneration,
                pckSha256 = identity.PckSha256,
                sourceAssemblySha256 = identity.SourceAssemblySha256,
            };

            File.WriteAllText(
                Path.Combine(runtimePackDirectory, "compatibility.json"),
                JsonSerializer.Serialize(new
                {
                    schemaVersion = 3,
                    packId,
                    gameIdentityId = identity.Id,
                    gameIdentity = declaredIdentity,
                    sourceBranch = identity.Branch,
                    installGeneration = identity.InstallGeneration,
                    sourcePckSha256 = identity.PckSha256,
                    sourceAssemblySha256 = identity.SourceAssemblySha256,
                    androidAssemblySha256,
                    supportAssemblies,
                    supportAssemblySha256,
                    patchSetVersion = PatchCompatibilityValidator.PatchSetVersion,
                    patchValidationStatus = "passed",
                    patchValidationReport = "patch_validation.json",
                    androidAssemblyFile = "sts2.dll",
                    validationMode = "static-critical-symbol-scan",
                    validationSurfaceVersion = validationSurface,
                    checkedSymbolCount = 1,
                    presentSymbolCount = 1,
                    missingSymbolCount = 0,
                    generatedFromCleanDirectory = true,
                })
            );
            File.WriteAllText(
                Path.Combine(runtimePackDirectory, "patch_validation.json"),
                JsonSerializer.Serialize(new
                {
                    schemaVersion = 3,
                    status = "passed",
                    runtimePackId = packId,
                    gameIdentityId = reportGameIdentityId ?? identity.Id,
                    gameIdentity = declaredIdentity,
                    branch = identity.Branch,
                    installGeneration = identity.InstallGeneration,
                    pckSha256 = identity.PckSha256,
                    sourceAssemblySha256 = identity.SourceAssemblySha256,
                    androidAssemblySha256,
                    patchSetVersion = PatchCompatibilityValidator.PatchSetVersion,
                    validationMode = PatchCompatibilityValidator.ValidationMode,
                    validationSurfaceVersion = validationSurface,
                    checkedSymbolCount = 1,
                    presentSymbolCount = 1,
                    missingSymbolCount = 0,
                    symbolChecks = new[]
                    {
                        new { Present = true },
                    },
                    missingSymbols = Array.Empty<string>(),
                    supportAssemblies,
                    supportAssemblySha256,
                    generatedFromCleanDirectory = true,
                })
            );
            return identity;
        }

        private static string HashFile(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(DataDir))
                    Directory.Delete(DataDir, recursive: true);
            }
            catch
            {
            }
        }
    }

    private static StringComparer PathComparer()
        => OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
}
