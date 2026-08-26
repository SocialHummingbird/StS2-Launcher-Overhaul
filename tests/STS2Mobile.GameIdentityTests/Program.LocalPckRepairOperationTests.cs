using System;
using System.IO;
using System.Linq;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void LocalPckRepairCompletesWithIsolatedInvalidation()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );
        var originalState = BranchInstallStateStore.Current.Read(
            fixture.DataDir,
            fixture.Branch
        );
        var originalPck = File.ReadAllBytes(fixture.PckPath);
        var originalSource = File.ReadAllBytes(fixture.SourceAssemblyPath);
        var originalMarker = File.ReadAllBytes(fixture.BranchMarkerPath);
        var originalMarkerTimestamp = File.GetLastWriteTimeUtc(
            fixture.BranchMarkerPath
        );
        var selectedPack = SeedRuntimeArtifacts(fixture.DataDir, fixture.Branch);
        var siblingPack = SeedRuntimeArtifacts(fixture.DataDir, SteamGameBranch.Public);
        var siblingManifest = Path.Combine(siblingPack, "compatibility.json");
        var siblingBytes = File.ReadAllBytes(siblingManifest);
        var savePath = WriteSaveSentinel(fixture.DataDir);
        var saveBytes = File.ReadAllBytes(savePath);
        BranchInstallState? observedBeforeReady = null;

        var completion = LocalPckRepairOperation.RepairAutomatic(
            fixture.DataDir,
            fixture.Branch,
            identity =>
            {
                observedBeforeReady = BranchInstallStateStore.Current.Read(
                    fixture.DataDir,
                    fixture.Branch
                );
                Equal(
                    BranchInstallStatus.Updating,
                    observedBeforeReady.Status,
                    "The completion callback must observe updating state."
                );
                Equal(
                    string.Empty,
                    observedBeforeReady.PckPreparationVersion,
                    "android-pck-v2 must not be published before completion."
                );
                Equal(
                    identity,
                    GameIdentityReader.ReadInstalledReadOnly(
                        fixture.DataDir,
                        fixture.Branch
                    ),
                    "The recalculated identity must already describe the repaired files."
                );
            }
        );

        var ready = BranchInstallStateStore.Current.Read(
            fixture.DataDir,
            fixture.Branch
        );
        Equal(BranchInstallStatus.Ready, ready.Status, "Repair must finish ready.");
        Equal(
            AndroidPckPreparationVersions.V2,
            ready.PckPreparationVersion,
            "Repair must publish exact android-pck-v2 readiness."
        );
        Equal(completion.GameIdentity, ready.GameIdentity, "Ready must publish the recalculated identity.");
        Equal(completion.TransactionId, ready.TransactionId, "Completion must use its locked update transaction.");
        NotEqual(originalState.GameIdentity, completion.GameIdentity, "Repair must change PCK-bound identity.");
        Equal(
            originalState.GameIdentity.InstallGeneration,
            completion.GameIdentity.InstallGeneration,
            "Timestamp refresh must not change the marker-byte install generation."
        );
        True(
            LauncherGameFiles.HasArm64V2PckPostconditions(
                fixture.DataDir,
                fixture.Branch
            ),
            "Every ARM64 v2 managed PCK postcondition must hold."
        );
        Equal(
            InstalledGameVersionReadiness.Ready,
            LauncherGameFiles.ClassifyInstalledVersion(
                fixture.DataDir,
                fixture.Branch
            ),
            "The repaired installation must classify as ready."
        );
        True(
            !originalPck.SequenceEqual(File.ReadAllBytes(fixture.PckPath)),
            "The managed PCK patcher must repair mixed v1 forms."
        );
        True(
            originalSource.SequenceEqual(File.ReadAllBytes(fixture.SourceAssemblyPath)),
            "Local PCK repair must not mutate the source game assembly."
        );
        True(
            originalMarker.SequenceEqual(File.ReadAllBytes(fixture.BranchMarkerPath)),
            "Marker refresh must preserve every marker byte."
        );
        True(
            File.GetLastWriteTimeUtc(fixture.BranchMarkerPath) > originalMarkerTimestamp,
            "Marker refresh must advance its timestamp."
        );
        True(!Directory.Exists(selectedPack), "Only the selected branch's old runtime pack must be invalidated.");
        True(
            siblingBytes.SequenceEqual(File.ReadAllBytes(siblingManifest)),
            "A sibling branch's runtime artifacts must remain unchanged."
        );
        True(
            saveBytes.SequenceEqual(File.ReadAllBytes(savePath)),
            "Local PCK repair must preserve saves."
        );
        True(observedBeforeReady != null, "The test must observe the pre-publication boundary.");

        using var exclusive = new FileStream(
            fixture.PckPath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None
        );
        True(exclusive.Length > 0, "The repaired PCK must be flushed and closed on return.");
    }

    private static void LocalPckRepairRegeneratesRuntimeAndPreservesUnrelatedState()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );
        File.Copy(
            typeof(Program).Assembly.Location,
            fixture.SourceAssemblyPath,
            overwrite: true
        );
        File.SetLastWriteTimeUtc(
            fixture.SourceAssemblyPath,
            DateTime.UtcNow.AddMinutes(-2)
        );
        var originalState = BranchInstallStateStore.Current.Read(
            fixture.DataDir,
            fixture.Branch
        );
        var originalIdentity = GameIdentityReader.ReadInstalledReadOnly(
            fixture.DataDir,
            fixture.Branch
        );
        var selectedTransaction = Guid.NewGuid();
        BranchInstallStateStore.Current.BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            selectedTransaction,
            "repair-regeneration-fixture",
            originalState.Depots
        );
        BranchInstallStateStore.Current.CommitReady(
            fixture.DataDir,
            fixture.Branch,
            selectedTransaction,
            originalIdentity,
            originalState.Depots,
            AndroidPckPreparationVersions.V1
        );

        var originalCandidate = GenerateRuntimePackCandidate(
            fixture,
            originalIdentity
        );
        True(originalCandidate.Succeeded, originalCandidate.Problem);
        var originalLaunch = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            originalIdentity,
            originalCandidate.Candidate,
            new RecordingCachePreparer()
        );
        True(originalLaunch.Succeeded, originalLaunch.Problem);
        var originalPackId = originalLaunch.RuntimeSlot.RuntimePack.PackId;
        var authorizationPath = LauncherRuntimeSlotEvidence.MarkerPath(
            fixture.DataDir
        );
        var originalAuthorization = File.ReadAllBytes(authorizationPath);

        SeedDownloadState(fixture);
        var selectedDownloadStatePath = Path.Combine(
            SteamGameInstallPaths.DownloadStateDirectoryPath(
                fixture.DataDir,
                fixture.Branch
            ),
            "123.id"
        );
        var selectedDownloadState = File.ReadAllBytes(selectedDownloadStatePath);
        var savePath = WriteSaveSentinel(fixture.DataDir);
        var saveBytes = File.ReadAllBytes(savePath);
        var credentialPath = Path.Combine(
            fixture.DataDir,
            "steam_credentials.enc"
        );
        var credentialBytes = Enumerable.Range(0, 193)
            .Select(index => unchecked((byte)(index * 29)))
            .ToArray();
        File.WriteAllBytes(credentialPath, credentialBytes);

        var publicInstall = new GameInstallFixture(
            fixture.DataDir,
            SteamGameBranch.Public
        );
        publicInstall.WriteSourceAssembly("public-source-before-stale-change");
        WriteReadinessPck(
            publicInstall.PckPath,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );
        WriteReadinessBranchMarker(publicInstall, 2001);
        var publicIdentity = GameIdentityReader.ReadInstalled(
            publicInstall.DataDir,
            publicInstall.Branch
        );
        var publicDepots = new[]
        {
            new BranchInstallDepot(123, 2001, "selected"),
        };
        var publicTransaction = Guid.NewGuid();
        BranchInstallStateStore.Current.BeginUpdating(
            publicInstall.DataDir,
            publicInstall.Branch,
            publicTransaction,
            "stale-public-fixture",
            publicDepots
        );
        BranchInstallStateStore.Current.CommitReady(
            publicInstall.DataDir,
            publicInstall.Branch,
            publicTransaction,
            publicIdentity,
            publicDepots,
            AndroidPckPreparationVersions.V1
        );
        publicInstall.WriteSourceAssembly("public-source-after-stale-change");
        SeedDownloadState(publicInstall);
        var publicPack = SeedRuntimeArtifacts(
            publicInstall.DataDir,
            publicInstall.Branch
        );
        var publicSnapshot = SnapshotBranch(publicInstall, publicPack);
        var publicMarker = File.ReadAllBytes(publicInstall.BranchMarkerPath);
        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(
                publicInstall.DataDir,
                publicInstall.Branch
            ),
            "The unrelated public branch must begin stale and ineligible for automatic repair."
        );

        var markerBytes = File.ReadAllBytes(fixture.BranchMarkerPath);
        var completion = LocalPckRepairOperation.RepairAutomatic(
            fixture.DataDir,
            fixture.Branch
        );

        True(
            markerBytes.SequenceEqual(File.ReadAllBytes(fixture.BranchMarkerPath)),
            "Repair must preserve the selected branch marker bytes."
        );
        Equal(
            originalIdentity.InstallGeneration,
            completion.GameIdentity.InstallGeneration,
            "Repair must preserve the marker-derived install generation."
        );
        True(
            File.GetLastWriteTimeUtc(fixture.BranchMarkerPath)
                > File.GetLastWriteTimeUtc(fixture.PckPath),
            "The marker timestamp must be newer than the flushed repaired PCK."
        );
        NotEqual(
            originalIdentity.PckSha256,
            completion.GameIdentity.PckSha256,
            "Repair must change the authoritative PCK hash."
        );
        NotEqual(
            originalIdentity,
            completion.GameIdentity,
            "Repair must publish a new PCK-bound GameIdentity."
        );
        Equal(
            originalIdentity.SourceAssemblySha256,
            completion.GameIdentity.SourceAssemblySha256,
            "Repair must not change the authoritative source DLL hash."
        );
        True(
            !LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir),
            "Repair must revoke the selected branch's old launch authorization."
        );

        var repairedCandidate = GenerateRuntimePackCandidate(
            fixture,
            completion.GameIdentity
        );
        True(repairedCandidate.Succeeded, repairedCandidate.Problem);
        var repairedLaunch = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            completion.GameIdentity,
            repairedCandidate.Candidate,
            new RecordingCachePreparer()
        );
        True(repairedLaunch.Succeeded, repairedLaunch.Problem);
        NotEqual(
            originalPackId,
            repairedLaunch.RuntimeSlot.RuntimePack.PackId,
            "The repaired identity must generate a different runtime pack."
        );
        True(
            LauncherRuntimeSlotEvidence.IsAuthorized(
                fixture.DataDir,
                completion.GameIdentity,
                repairedLaunch.RuntimeSlot.RuntimePack.PackId,
                out var authorizationProblem
            ),
            $"The regenerated runtime pack must be authorized: {authorizationProblem}"
        );
        True(
            !originalAuthorization.SequenceEqual(File.ReadAllBytes(authorizationPath)),
            "Launch authorization must be regenerated for the repaired identity."
        );

        True(
            saveBytes.SequenceEqual(File.ReadAllBytes(savePath)),
            "Repair and runtime regeneration must preserve saves."
        );
        True(
            credentialBytes.SequenceEqual(File.ReadAllBytes(credentialPath)),
            "Repair and runtime regeneration must preserve credentials."
        );
        True(
            selectedDownloadState.SequenceEqual(
                File.ReadAllBytes(selectedDownloadStatePath)
            ),
            "Repair and runtime regeneration must preserve download state."
        );
        AssertBranchSnapshot(publicInstall, publicPack, publicSnapshot);
        True(
            publicMarker.SequenceEqual(
                File.ReadAllBytes(publicInstall.BranchMarkerPath)
            ),
            "Repair must preserve the unrelated stale public branch marker."
        );
        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(
                publicInstall.DataDir,
                publicInstall.Branch
            ),
            "The unrelated stale public branch must not be silently repaired."
        );
    }

    private static void LocalPckRepairRepeatedCallIsNoOp()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );
        var first = LocalPckRepairOperation.RepairAutomatic(
            fixture.DataDir,
            fixture.Branch
        );
        var statePath = BranchInstallStateStore.PathFor(
            fixture.DataDir,
            fixture.Branch
        );
        var paths = new[]
        {
            fixture.PckPath,
            fixture.SourceAssemblyPath,
            fixture.BranchMarkerPath,
            statePath,
        };
        var bytesBefore = paths.ToDictionary(path => path, File.ReadAllBytes);
        var timestampsBefore = paths.ToDictionary(path => path, File.GetLastWriteTimeUtc);
        var selectedPack = SeedRuntimeArtifacts(fixture.DataDir, fixture.Branch);
        var runtimeSentinel = Path.Combine(selectedPack, "compatibility.json");
        var runtimeBytes = File.ReadAllBytes(runtimeSentinel);

        var second = LocalPckRepairOperation.RepairAutomatic(
            fixture.DataDir,
            fixture.Branch
        );

        Equal(first.TransactionId, second.TransactionId, "No-op repair must retain the ready transaction.");
        Equal(first.GameIdentity, second.GameIdentity, "No-op repair must retain the ready identity.");
        foreach (var path in paths)
        {
            True(
                bytesBefore[path].SequenceEqual(File.ReadAllBytes(path)),
                $"Repeated repair must not change {path}."
            );
            Equal(
                timestampsBefore[path],
                File.GetLastWriteTimeUtc(path),
                $"Repeated repair must not touch the timestamp for {path}."
            );
        }
        True(
            runtimeBytes.SequenceEqual(File.ReadAllBytes(runtimeSentinel)),
            "Repeated repair must not invalidate runtime artifacts again."
        );
    }

    private static void LocalPckRepairResumesBeforePckMutation()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );
        Guid interruptedTransaction;
        using (var interrupted = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            targetDepots: null,
            phase: LocalPckRepairOperation.StartingPhase
        ))
        {
            interruptedTransaction = interrupted.TransactionId;
        }

        AssertLocalPckRepairInterrupted(fixture, "An interruption after marking updating must fail closed.");
        var completion = LocalPckRepairOperation.RepairAutomatic(
            fixture.DataDir,
            fixture.Branch
        );

        Equal(
            interruptedTransaction,
            completion.TransactionId,
            "Repair must resume the existing interrupted transaction."
        );
        AssertLocalPckRepairReady(fixture, completion);
    }

    private static void LocalPckRepairResumesBeforeReadyPublication()
    {
        using var fixture = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        );
        var markerBytes = File.ReadAllBytes(fixture.BranchMarkerPath);
        var selectedPack = SeedRuntimeArtifacts(fixture.DataDir, fixture.Branch);

        Throws<SimulatedLocalPckRepairInterruptionException>(() =>
            LocalPckRepairOperation.RepairAutomatic(
                fixture.DataDir,
                fixture.Branch,
                _ => throw new SimulatedLocalPckRepairInterruptionException()
            )
        );

        var interrupted = BranchInstallStateStore.Current.Read(
            fixture.DataDir,
            fixture.Branch
        );
        AssertLocalPckRepairInterrupted(
            fixture,
            "An interruption immediately before v2 publication must fail closed."
        );
        True(
            interrupted.Phase.StartsWith(
                LocalPckRepairOperation.PhasePrefix + "-",
                StringComparison.Ordinal
            ),
            "Interrupted repair must retain its existing repair phase for safe repetition."
        );
        True(
            LauncherGameFiles.HasArm64V2PckPostconditions(
                fixture.DataDir,
                fixture.Branch
            ),
            "The pre-publication interruption must leave a complete, repeatable v2 PCK."
        );
        True(
            markerBytes.SequenceEqual(File.ReadAllBytes(fixture.BranchMarkerPath)),
            "Interrupted repair must preserve branch marker bytes."
        );
        True(!Directory.Exists(selectedPack), "Old selected runtime artifacts must already be invalidated.");

        var completion = LocalPckRepairOperation.RepairAutomatic(
            fixture.DataDir,
            fixture.Branch
        );

        Equal(
            interrupted.TransactionId,
            completion.TransactionId,
            "Retry must finish the same interrupted repair transaction."
        );
        AssertLocalPckRepairReady(fixture, completion);
    }

    private static void LocalPckRepairRejectsUnknownAndTornEntries()
    {
        var unknownEntries = ManagedReadinessEntries(
            includeProjectGodot: true
        ).ToArray();
        unknownEntries[1] = (
            ManagedFmodPckForms.ProjectGodotPath,
            "FmodManager=\"*res://addons/fmod/unknown.gd\""
        );
        using (var unknown = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            unknownEntries
        ))
        {
            AssertLocalPckRepairRejectedWithoutMutation(
                unknown,
                "An unknown managed FMOD entry"
            );
        }

        using (var torn = CreateReadinessFixture(
            AndroidPckPreparationVersions.V1,
            ManagedReadinessEntries(mixedRecognizedForms: true)
        ))
        {
            using (var stream = new FileStream(
                torn.PckPath,
                FileMode.Open,
                FileAccess.Write,
                FileShare.None
            ))
            {
                stream.SetLength(stream.Length - 1);
                stream.Flush(flushToDisk: true);
            }
            AssertLocalPckRepairRejectedWithoutMutation(
                torn,
                "A torn managed PCK entry"
            );
        }
    }

    private static void AssertLocalPckRepairRejectedWithoutMutation(
        GameInstallFixture fixture,
        string scenario
    )
    {
        var statePath = BranchInstallStateStore.PathFor(
            fixture.DataDir,
            fixture.Branch
        );
        var pckBytes = File.ReadAllBytes(fixture.PckPath);
        var stateBytes = File.ReadAllBytes(statePath);
        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(
                fixture.DataDir,
                fixture.Branch
            ),
            $"{scenario} must classify as RedownloadRequired."
        );
        Throws<IOException>(() => LocalPckRepairOperation.RepairAutomatic(
            fixture.DataDir,
            fixture.Branch
        ));
        True(
            pckBytes.SequenceEqual(File.ReadAllBytes(fixture.PckPath)),
            $"{scenario} must not be mutated by local repair."
        );
        True(
            stateBytes.SequenceEqual(File.ReadAllBytes(statePath)),
            $"{scenario} must not begin an update transaction."
        );
    }

    private static void AssertLocalPckRepairInterrupted(
        GameInstallFixture fixture,
        string message
    )
    {
        var state = BranchInstallStateStore.Current.Read(
            fixture.DataDir,
            fixture.Branch
        );
        Equal(BranchInstallStatus.Updating, state.Status, message);
        Equal(
            InstalledGameVersionReadiness.RedownloadRequired,
            LauncherGameFiles.ClassifyInstalledVersion(
                fixture.DataDir,
                fixture.Branch
            ),
            "An interrupted operation must not classify as ready or automatic repair."
        );
    }

    private static void AssertLocalPckRepairReady(
        GameInstallFixture fixture,
        BranchInstallCompletion completion
    )
    {
        var state = BranchInstallStateStore.Current.Read(
            fixture.DataDir,
            fixture.Branch
        );
        Equal(BranchInstallStatus.Ready, state.Status, "Retry must finish ready.");
        Equal(
            AndroidPckPreparationVersions.V2,
            state.PckPreparationVersion,
            "Retry must publish exact android-pck-v2 readiness."
        );
        Equal(completion.GameIdentity, state.GameIdentity, "Retry must publish its recalculated identity.");
        True(
            LauncherGameFiles.HasArm64V2PckPostconditions(
                fixture.DataDir,
                fixture.Branch
            ),
            "Retry must retain every ARM64 v2 postcondition."
        );
    }

    private sealed class SimulatedLocalPckRepairInterruptionException : IOException
    {
    }
}
