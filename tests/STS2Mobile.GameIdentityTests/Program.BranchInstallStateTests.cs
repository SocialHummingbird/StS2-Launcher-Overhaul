using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void BranchStateUpdatingToReadyTransition()
    {
        using var fixture = BranchStateFixture.Create();
        var clock = new SequenceClock(
            DateTimeOffset.Parse("2026-08-22T12:00:00Z"),
            DateTimeOffset.Parse("2026-08-22T12:03:00Z")
        );
        var store = NewStateStore(clock.Next);
        var transactionId = Guid.NewGuid();
        var depots = new[]
        {
            new BranchInstallDepot(456, 9002, "selected"),
            new BranchInstallDepot(123, 9001, "selected"),
        };

        var updating = store.BeginUpdating(
            fixture.DataDir,
            "  PUBLIC-BETA  ",
            transactionId,
            "downloading",
            depots
        );
        Equal(BranchInstallStatus.Updating, updating.Status, "BeginUpdating must durably revoke readiness.");
        Equal("public-beta", updating.Branch, "State branch must use normalized storage identity.");
        Equal<ulong>(123, updating.Depots[0].DepotId, "Depot rows must be serialized in numeric order.");
        True(
            !store.TryReadReady(fixture.DataDir, fixture.Branch, StateIdentity(fixture.Branch, "N"), out _, out _),
            "Updating state must never authorize launch."
        );

        var identity = StateIdentity(fixture.Branch, "N");
        var ready = store.CommitReady(
            fixture.DataDir,
            fixture.Branch,
            transactionId,
            identity,
            depots,
            "android-pck-v1",
            StateRuntimePack(identity)
        );
        Equal(BranchInstallStatus.Ready, ready.Status, "CommitReady must persist the only ready representation.");
        Equal(identity.InstallGeneration, ready.GameIdentity.InstallGeneration, "Ready state must identify the exact install generation.");
        True(
            store.TryReadReady(fixture.DataDir, fixture.Branch, identity, out var reread, out var problem),
            $"Matching ready identity should authorize readiness: {problem}"
        );
        Equal(identity, reread.GameIdentity, "Ready state must round-trip the complete GameIdentity.");

        var stateBytes = File.ReadAllBytes(BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch));
        True(stateBytes.Length > 0 && !(stateBytes[0] == 0xef && stateBytes.Length > 2 && stateBytes[1] == 0xbb && stateBytes[2] == 0xbf), "State JSON must be UTF-8 without BOM.");
        using var document = JsonDocument.Parse(stateBytes);
        Equal("ready", document.RootElement.GetProperty("status").GetString(), "On-disk status must be ready.");
        Equal(identity.InstallGeneration, document.RootElement.GetProperty("gameIdentity").GetProperty("installGeneration").GetString(), "On-disk identity must include install generation.");
    }

    private static void BranchStateInterruptedAtomicWrite()
    {
        using var fixture = BranchStateFixture.Create();
        var normalStore = NewStateStore();
        var transactionId = Guid.NewGuid();
        var identity = StateIdentity(fixture.Branch, "N");
        normalStore.BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            transactionId,
            "finalizing",
            StateDepots()
        );

        var hookObservedStagedFile = false;
        var interruptedStore = new BranchInstallStateStore(
            new AtomicFileWriter((temporaryPath, targetPath) =>
            {
                hookObservedStagedFile = File.Exists(temporaryPath);
                Equal(
                    "updating",
                    ReadStateStatus(targetPath),
                    "The target must remain updating until atomic replacement."
                );
                throw new SimulatedProcessInterruptionException();
            })
        );
        Throws<SimulatedProcessInterruptionException>(() => interruptedStore.CommitReady(
            fixture.DataDir,
            fixture.Branch,
            transactionId,
            identity,
            StateDepots(),
            "android-pck-v1",
            StateRuntimePack(identity)
        ));

        True(hookObservedStagedFile, "Atomic persistence must stage and flush a same-directory temporary file before replacement.");
        Equal(BranchInstallStatus.Updating, normalStore.Read(fixture.DataDir, fixture.Branch).Status, "Interrupted ready write must leave the durable target updating.");

        var orphan = BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch) + ".deadbeef.tmp";
        File.WriteAllText(orphan, "{\"schemaVersion\":1", Encoding.UTF8);
        Equal(BranchInstallStatus.Updating, normalStore.Read(fixture.DataDir, fixture.Branch).Status, "Readers must ignore orphaned temporary files.");
    }

    private static void BranchStateInterruptedReadyRevocation()
    {
        using var fixture = BranchStateFixture.Create();
        var store = NewStateStore();
        var readyTransaction = Guid.NewGuid();
        var identity = StateIdentity(fixture.Branch, "N");
        store.BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            readyTransaction,
            "seeding-ready",
            StateDepots()
        );
        store.CommitReady(
            fixture.DataDir,
            fixture.Branch,
            readyTransaction,
            identity,
            StateDepots(),
            "android-pck-v1"
        );
        var statePath = BranchInstallStateStore.PathFor(
            fixture.DataDir,
            fixture.Branch
        );
        var readyBytes = File.ReadAllBytes(statePath);

        var interrupted = new BranchInstallStateStore(
            new AtomicFileWriter((temporaryPath, targetPath) =>
            {
                True(File.Exists(temporaryPath), "Updating revocation must be fully staged before replacement.");
                throw new SimulatedProcessInterruptionException();
            })
        );
        Throws<SimulatedProcessInterruptionException>(() =>
            interrupted.BeginUpdating(
                fixture.DataDir,
                fixture.Branch,
                Guid.NewGuid(),
                "downloading",
                StateDepots()
            )
        );

        True(
            readyBytes.SequenceEqual(File.ReadAllBytes(statePath)),
            "An interrupted atomic revocation must leave the prior ready document intact; no installed mutation has been authorized yet."
        );
        True(
            store.TryReadReady(
                fixture.DataDir,
                fixture.Branch,
                identity,
                out _,
                out _
            ),
            "The previous ready state remains readable only because the downloader never crossed the mutation guard."
        );

        var updating = store.BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            Guid.NewGuid(),
            "downloading",
            StateDepots()
        );
        Equal(BranchInstallStatus.Updating, updating.Status, "A retry must durably revoke ready before mutation.");
    }

    private static void BranchStateCorruptOrTruncated()
    {
        using var fixture = BranchStateFixture.Create();
        var store = NewStateStore();
        var path = BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch);
        var missing = Throws<BranchInstallStateException>(() => store.Read(fixture.DataDir, fixture.Branch));
        Equal(BranchInstallStateFailureKind.Missing, missing.Kind, "Missing state must fail closed.");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"schemaVersion\":1", Encoding.UTF8);
        var truncated = Throws<BranchInstallStateException>(() => store.Read(fixture.DataDir, fixture.Branch));
        Equal(BranchInstallStateFailureKind.Corrupt, truncated.Kind, "Truncated state must fail closed as corrupt.");

        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 99,
                branch = fixture.Branch,
                status = "ready",
            }),
            Encoding.UTF8
        );
        var unknownSchema = Throws<BranchInstallStateException>(() => store.Read(fixture.DataDir, fixture.Branch));
        Equal(BranchInstallStateFailureKind.UnknownSchema, unknownSchema.Kind, "Unknown state schema must fail closed.");

        File.WriteAllText(
            path,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                branch = fixture.Branch,
                status = "ready-ish",
                transactionId = Guid.NewGuid().ToString("D"),
            }),
            Encoding.UTF8
        );
        var unknown = Throws<BranchInstallStateException>(() => store.Read(fixture.DataDir, fixture.Branch));
        Equal(BranchInstallStateFailureKind.Corrupt, unknown.Kind, "Unknown lifecycle status must fail closed.");
        True(
            !store.TryReadReady(fixture.DataDir, fixture.Branch, StateIdentity(fixture.Branch, "N"), out _, out _),
            "Corrupt state must not authorize readiness."
        );
    }

    private static void BranchStateLegacyEvidence()
    {
        using var fixture = BranchStateFixture.Create();
        var legacyMarker = SteamGameInstallPaths.BranchMarkerPath(fixture.DataDir, fixture.Branch);
        Directory.CreateDirectory(Path.GetDirectoryName(legacyMarker)!);
        File.WriteAllText(legacyMarker, $"Branch: {fixture.Branch}\nDepot manifest count: 1\n", Encoding.UTF8);

        var legacyMarkerFailure = Throws<BranchInstallStateException>(() => NewStateStore().Read(fixture.DataDir, fixture.Branch));
        Equal(BranchInstallStateFailureKind.LegacyRequiresRecovery, legacyMarkerFailure.Kind, "Legacy ready marker cannot establish readiness.");

        var statePath = BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch);
        Directory.CreateDirectory(Path.GetDirectoryName(statePath)!);
        File.WriteAllText(statePath, "{\"branch\":\"public-beta\",\"status\":\"ready\"}", Encoding.UTF8);
        var legacyStateFailure = Throws<BranchInstallStateException>(() => NewStateStore().Read(fixture.DataDir, fixture.Branch));
        Equal(BranchInstallStateFailureKind.LegacyRequiresRecovery, legacyStateFailure.Kind, "Schema-less ready state must require recovery.");

        var recovered = NewStateStore().BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            Guid.NewGuid(),
            "migrating",
            StateDepots()
        );
        Equal(BranchInstallStatus.Updating, recovered.Status, "Migration must first persist updating instead of trusting legacy readiness.");
    }

    private static void BranchStateIdentityGenerationMismatch()
    {
        using var fixture = BranchStateFixture.Create();
        var store = NewStateStore();
        var transactionId = Guid.NewGuid();
        var generationN = StateIdentity(fixture.Branch, "generation-N", "same-files");
        var generationNPlusOne = StateIdentity(fixture.Branch, "generation-N+1", "same-files");
        store.BeginUpdating(fixture.DataDir, fixture.Branch, transactionId, "finalizing", StateDepots());
        store.CommitReady(
            fixture.DataDir,
            fixture.Branch,
            transactionId,
            generationN,
            StateDepots(),
            "android-pck-v1",
            StateRuntimePack(generationN)
        );

        True(
            !store.TryReadReady(fixture.DataDir, fixture.Branch, generationNPlusOne, out _, out var problem),
            "A different completed generation must invalidate readiness even when file hashes are equal."
        );
        Contains(problem, "generation mismatch", "Mismatch diagnostics should identify the generation.");
        var mismatch = Throws<BranchInstallStateException>(() => store.RequireReady(fixture.DataDir, fixture.Branch, generationNPlusOne));
        Equal(BranchInstallStateFailureKind.IdentityMismatch, mismatch.Kind, "Generation mismatch must fail closed.");
    }

    private static void BranchStateSlotsAreIndependent()
    {
        using var fixture = BranchStateFixture.Create();
        var store = NewStateStore();
        var publicIdentity = StateIdentity("public", "public-N");
        var betaIdentity = StateIdentity("public-beta", "beta-N");
        var publicTransaction = Guid.NewGuid();
        var betaTransaction = Guid.NewGuid();

        store.BeginUpdating(fixture.DataDir, "PUBLIC", publicTransaction, "downloading", StateDepots(100));
        store.CommitReady(fixture.DataDir, "public", publicTransaction, publicIdentity, StateDepots(100), "android-pck-v1", StateRuntimePack(publicIdentity));
        store.BeginUpdating(fixture.DataDir, " Public-Beta ", betaTransaction, "downloading", StateDepots(200));
        store.CommitReady(fixture.DataDir, "public-beta", betaTransaction, betaIdentity, StateDepots(200), "android-pck-v1", StateRuntimePack(betaIdentity));

        var publicPath = BranchInstallStateStore.PathFor(fixture.DataDir, "public");
        var betaPath = BranchInstallStateStore.PathFor(fixture.DataDir, "public-beta");
        NotEqual(Path.GetFullPath(publicPath), Path.GetFullPath(betaPath), "Branches must use distinct normalized slot state files.");
        True(store.TryReadReady(fixture.DataDir, "public", publicIdentity, out _, out _), "Public slot should remain independently ready.");
        True(store.TryReadReady(fixture.DataDir, "PUBLIC-BETA", betaIdentity, out _, out _), "Public-beta slot should remain independently ready.");

        store.BeginUpdating(fixture.DataDir, "public-beta", Guid.NewGuid(), "downloading", StateDepots(200));
        True(store.TryReadReady(fixture.DataDir, "public", publicIdentity, out _, out _), "Updating public-beta must not revoke public readiness.");
        Equal(BranchInstallStatus.Updating, store.Read(fixture.DataDir, "public-beta").Status, "Only selected beta slot should be updating.");

        File.Copy(betaPath, publicPath, overwrite: true);
        var misplaced = Throws<BranchInstallStateException>(() => store.Read(fixture.DataDir, "public"));
        Equal(BranchInstallStateFailureKind.BranchMismatch, misplaced.Kind, "A state document from another normalized branch slot must fail closed.");
    }

    private static void BranchStateRepeatedAndConcurrentTransitions()
    {
        using var fixture = BranchStateFixture.Create();
        var clock = new SequenceClock(
            DateTimeOffset.Parse("2026-08-22T12:00:00Z"),
            DateTimeOffset.Parse("2026-08-22T12:05:00Z")
        );
        var store = NewStateStore(clock.Next);
        var transactionId = Guid.NewGuid();
        var first = store.BeginUpdating(fixture.DataDir, fixture.Branch, transactionId, "downloading", StateDepots());
        var calls = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() => store.BeginUpdating(
                fixture.DataDir,
                fixture.Branch,
                transactionId,
                "downloading",
                StateDepots()
            )))
            .ToArray();
        Task.WaitAll(calls);
        True(calls.All(call => call.Result.TransitionUtc == first.TransitionUtc), "Repeated concurrent transition must preserve the original transaction start.");

        var conflict = Throws<BranchInstallStateException>(() => store.BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            Guid.NewGuid(),
            "downloading",
            StateDepots()
        ));
        Equal(BranchInstallStateFailureKind.InvalidTransition, conflict.Kind, "A concurrent different transaction must not replace the active transaction.");

        var identity = StateIdentity(fixture.Branch, "N");
        var ready = store.CommitReady(fixture.DataDir, fixture.Branch, transactionId, identity, StateDepots(), "android-pck-v1", StateRuntimePack(identity));
        var repeatedReady = store.CommitReady(fixture.DataDir, fixture.Branch, transactionId, identity, StateDepots(), "android-pck-v1", StateRuntimePack(identity));
        Equal(ready.TransitionUtc, repeatedReady.TransitionUtc, "Repeated identical ready commit must be idempotent.");
        Equal(BranchInstallStatus.Ready, store.Read(fixture.DataDir, fixture.Branch).Status, "Concurrent/repeated transitions must leave one valid state.");
    }

    private static BranchInstallStateStore NewStateStore(Func<DateTimeOffset>? utcNow = null)
        => new(new AtomicFileWriter(), utcNow);

    private static BranchInstallDepot[] StateDepots(ulong depotId = 123)
        => new[] { new BranchInstallDepot(depotId, depotId + 1000, "selected") };

    private static GameIdentity StateIdentity(
        string branch,
        string generationSeed,
        string fileSeed = "files"
    ) => new(
        branch,
        HashText(generationSeed),
        HashText($"pck:{fileSeed}"),
        HashText($"dll:{fileSeed}")
    );

    private static BranchInstallRuntimePack StateRuntimePack(GameIdentity identity)
        => new(
            $"pack-{identity.Id[..12]}",
            PatchCompatibilityValidator.PatchSetVersion,
            "critical-startup-save-platform-model-v1",
            HashText($"android:{identity.Id}")
        );

    private static string HashText(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string ReadStateStatus(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty("status").GetString() ?? string.Empty;
    }

    private sealed class SequenceClock
    {
        private readonly Queue<DateTimeOffset> _values;
        private DateTimeOffset _last;

        internal SequenceClock(params DateTimeOffset[] values)
        {
            _values = new Queue<DateTimeOffset>(values);
            _last = values.Last();
        }

        internal DateTimeOffset Next()
        {
            if (_values.Count > 0)
                _last = _values.Dequeue();
            return _last;
        }
    }

    private sealed class SimulatedProcessInterruptionException : IOException
    {
    }

    private sealed class BranchStateFixture : IDisposable
    {
        private BranchStateFixture(string dataDir)
        {
            DataDir = dataDir;
        }

        internal string DataDir { get; }
        internal string Branch => "public-beta";

        internal static BranchStateFixture Create()
        {
            var dataDir = Path.Combine(
                Path.GetTempPath(),
                "sts2-branch-state-tests",
                Guid.NewGuid().ToString("N")
            );
            Directory.CreateDirectory(dataDir);
            return new BranchStateFixture(dataDir);
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
}
