using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using STS2Mobile.Launcher;
using STS2Mobile.Steam;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void BranchLifecycleSuccessfulReplacement()
    {
        using var fixture = GameInstallFixture.Create();
        var generationN = fixture.WriteMatchingRuntimePack();
        PublishInitialReady(fixture, generationN, 1001);
        var activeAssembly = WriteActiveAssemblySentinel(fixture.DataDir);
        var save = WriteSaveSentinel(fixture.DataDir);
        var runtimePackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(
            fixture.DataDir,
            fixture.Branch
        );
        var initialReadiness = LauncherLaunchReadiness.Evaluate(
            fixture.DataDir,
            fixture.Branch,
            "lifecycle initial readiness"
        );
        True(initialReadiness.Ready, "Generation N should begin fully ready for the update regression.");

        BranchInstallCompletion completion;
        using (var update = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            Equal(BranchInstallStatus.Updating, update.State.Status, "Update must revoke ready before replacement.");
            True(Directory.Exists(runtimePackDirectory), "Previous derived pack should remain until completed files establish the new identity.");
            True(
                !LauncherGameFiles.DownloadedForValidation(fixture.DataDir, fixture.Branch, out _),
                "An updating slot must not launch even before its first file changes."
            );
            True(
                !LauncherLaunchReadiness.Evaluate(
                    fixture.DataDir,
                    fixture.Branch,
                    "lifecycle updating readiness"
                ).Ready,
                "A cached generation-N readiness result must not bypass updating state."
            );

            ReplaceWithGeneration(fixture, 1002, 0x42, "source-generation-two");
            completion = update.CompleteInstalledFiles("android-pck-v1");
        }

        NotEqual(generationN, completion.GameIdentity, "N to N+1 must publish a new authoritative identity.");
        True(
            BranchInstallStateStore.Current.TryReadReady(
                fixture.DataDir,
                fixture.Branch,
                completion.GameIdentity,
                out var ready,
                out var problem
            ),
            $"Completed N+1 files must publish matching ready state: {problem}"
        );
        Equal(completion.TransactionId, ready.TransactionId, "Completion must publish the same transaction that owned mutation.");
        True(!Directory.Exists(runtimePackDirectory), "The previous identity's selected-branch runtime pack must be invalidated before ready publication.");
        Equal("active-N", File.ReadAllText(activeAssembly), "The active assembly cache must survive until a new pack is validated.");
        Equal("save-data", File.ReadAllText(save), "A successful update must not touch saves.");
        var suppliedSlot = GameRuntimeSlot.Inspect(fixture.DataDir, completion.GameIdentity);
        Equal(completion.GameIdentity, suppliedSlot.GameIdentity, "Post-download runtime readiness must consume the published identity directly.");
    }

    private static void BranchLifecycleRestartBeforeFirstReplacement()
    {
        using var fixture = GameInstallFixture.Create();
        var generationN = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        PublishInitialReady(fixture, generationN, 1001);
        var oldPck = File.ReadAllBytes(fixture.PckPath);
        var oldDll = File.ReadAllBytes(fixture.SourceAssemblyPath);
        var save = WriteSaveSentinel(fixture.DataDir);
        Guid interruptedTransaction;

        using (var interrupted = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            interruptedTransaction = interrupted.TransactionId;
            interrupted.RecordFailure("failed-before-first-replacement", new IOException("network stopped"));
        }

        True(oldPck.SequenceEqual(File.ReadAllBytes(fixture.PckPath)), "Failure before replacement must preserve the old PCK.");
        True(oldDll.SequenceEqual(File.ReadAllBytes(fixture.SourceAssemblyPath)), "Failure before replacement must preserve the old DLL.");
        AssertUpdatingAndBlocked(fixture);

        using (var recovered = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            Equal(interruptedTransaction, recovered.TransactionId, "Restart must resume the interrupted transaction.");
            ReplaceWithGeneration(fixture, 1002, 0x43, "source-generation-two");
            recovered.CompleteInstalledFiles("android-pck-v1");
        }

        AssertReadyFromCurrentFiles(fixture);
        Equal("save-data", File.ReadAllText(save), "Failure and recovery must not touch saves.");
    }

    private static void BranchLifecycleRestartAfterOneReplacement()
    {
        using var fixture = GameInstallFixture.Create();
        var generationN = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        PublishInitialReady(fixture, generationN, 1001);
        var originalDll = File.ReadAllBytes(fixture.SourceAssemblyPath);
        var save = WriteSaveSentinel(fixture.DataDir);
        Guid interruptedTransaction;

        using (var interrupted = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            interruptedTransaction = interrupted.TransactionId;
            interrupted.RequireUpdatingBeforeInstalledMutation();
            fixture.WritePck(0x44);
        }

        True(originalDll.SequenceEqual(File.ReadAllBytes(fixture.SourceAssemblyPath)), "Only the first replacement should have occurred.");
        AssertUpdatingAndBlocked(fixture);

        using (var recovered = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            Equal(interruptedTransaction, recovered.TransactionId, "Restart after one replacement must resume the transaction.");
            fixture.WriteSourceAssembly("source-generation-two");
            fixture.CompleteGeneration(1002);
            recovered.CompleteInstalledFiles("android-pck-v1");
        }

        AssertReadyFromCurrentFiles(fixture);
        Equal("save-data", File.ReadAllText(save), "Partial replacement recovery must not touch saves.");
    }

    private static void BranchLifecycleRestartBeforeReadyPublication()
    {
        using var fixture = GameInstallFixture.Create();
        var generationN = fixture.WriteMatchingRuntimePack();
        PublishInitialReady(fixture, generationN, 1001);
        var activeAssembly = WriteActiveAssemblySentinel(fixture.DataDir);
        var save = WriteSaveSentinel(fixture.DataDir);
        Guid interruptedTransaction;

        using (var interrupted = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            interruptedTransaction = interrupted.TransactionId;
            ReplaceWithGeneration(fixture, 1002, 0x45, "source-generation-two");
            Throws<SimulatedLifecycleInterruptionException>(() =>
                interrupted.CompleteInstalledFiles(
                    "android-pck-v1",
                    _ => throw new SimulatedLifecycleInterruptionException()
                )
            );
        }

        AssertUpdatingAndBlocked(fixture);
        True(
            !Directory.Exists(GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch)),
            "Completed-file interruption should leave the stale runtime pack invalidated."
        );
        Equal("active-N", File.ReadAllText(activeAssembly), "Derived invalidation must preserve the active assembly cache.");

        using (var recovered = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            Equal(interruptedTransaction, recovered.TransactionId, "Restart before ready publication must resume the transaction.");
            recovered.CompleteInstalledFiles("android-pck-v1");
        }

        AssertReadyFromCurrentFiles(fixture);
        Equal("save-data", File.ReadAllText(save), "Finalization interruption and recovery must not touch saves.");
    }

    private static void BranchLifecycleRestartAfterCompletedFiles()
    {
        using var fixture = GameInstallFixture.Create();
        var identityN = GameIdentityReader.ReadInstalled(
            fixture.DataDir,
            fixture.Branch
        );
        PublishInitialReady(fixture, identityN, 1001);
        var save = WriteSaveSentinel(fixture.DataDir);
        Guid transactionId;

        using (var interrupted = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            transactionId = interrupted.TransactionId;
            ReplaceWithGeneration(
                fixture,
                1002,
                0x47,
                "source-generation-two"
            );
            interrupted.UpdatePhase("installed-files-complete-before-identity");
        }

        AssertUpdatingAndBlocked(fixture);
        using (var recovered = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            Equal(transactionId, recovered.TransactionId, "Restart after all file replacements must resume the same transaction.");
            recovered.CompleteInstalledFiles("android-pck-v1");
        }

        AssertReadyFromCurrentFiles(fixture);
        Equal("save-data", File.ReadAllText(save), "Restart after completed files must preserve saves.");
    }

    private static void BranchLifecycleRestartAfterInstallReady()
    {
        using var fixture = CreateRuntimePackFixture();
        var identityN = PublishRuntimePackFixtureReady(fixture);
        var candidateN = GenerateRuntimePackCandidate(fixture, identityN);
        True(candidateN.Succeeded, candidateN.Problem);
        True(
            RuntimePackLaunchLifecycle.Complete(
                fixture.DataDir,
                identityN,
                candidateN.Candidate,
                new RecordingCachePreparer()
            ).Succeeded,
            "Generation N must begin launchable."
        );

        GameIdentity identityNPlusOne;
        using (var update = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            File.Copy(
                typeof(Program).Assembly.Location,
                fixture.SourceAssemblyPath,
                overwrite: true
            );
            File.SetLastWriteTimeUtc(
                fixture.SourceAssemblyPath,
                DateTime.UtcNow.AddMinutes(-2)
            );
            fixture.WritePck(0x48);
            fixture.CompleteGeneration(1002);
            identityNPlusOne = update.CompleteInstalledFiles(
                "android-pck-v1"
            ).GameIdentity;
        }

        True(
            BranchInstallStateStore.Current.TryReadReady(
                fixture.DataDir,
                fixture.Branch,
                identityNPlusOne,
                out _,
                out _
            ),
            "Installed-file ready publication must survive a process stop before runtime-pack work."
        );
        True(
            !LauncherLaunchReadiness.Evaluate(
                fixture.DataDir,
                fixture.Branch,
                "restart after install ready"
            ).Ready,
            "Installed files alone must remain blocked without an N+1 runtime pack."
        );

        var candidateNPlusOne = GenerateRuntimePackCandidate(
            fixture,
            identityNPlusOne
        );
        True(candidateNPlusOne.Succeeded, candidateNPlusOne.Problem);
        var recovered = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identityNPlusOne,
            candidateNPlusOne.Candidate,
            new RecordingCachePreparer()
        );
        True(recovered.Succeeded, $"Restart must finish N+1 runtime readiness: {recovered.Problem}");
    }

    private static void BranchLifecyclePreservesOtherBranch()
    {
        using var beta = GameInstallFixture.Create("public-beta");
        var publicInstall = new GameInstallFixture(beta.DataDir, "public");
        Directory.CreateDirectory(Path.GetDirectoryName(publicInstall.SourceAssemblyPath)!);
        publicInstall.WritePck(0x31);
        publicInstall.WriteSourceAssembly("public-source-generation-one");
        publicInstall.CompleteGeneration(2001);

        var betaN = beta.WriteMatchingRuntimePack();
        var publicN = publicInstall.WriteMatchingRuntimePack();
        PublishInitialReady(beta, betaN, 1001);
        PublishInitialReady(publicInstall, publicN, 2001);
        var publicPck = File.ReadAllBytes(publicInstall.PckPath);
        var publicDll = File.ReadAllBytes(publicInstall.SourceAssemblyPath);
        var publicState = File.ReadAllBytes(
            BranchInstallStateStore.PathFor(beta.DataDir, "public")
        );
        var publicPackManifest = Path.Combine(
            GameRuntimeSlot.RuntimePackDirectoryPath(beta.DataDir, "public"),
            "compatibility.json"
        );
        var publicPack = File.ReadAllBytes(publicPackManifest);
        File.WriteAllText(
            LauncherRuntimeSlotEvidence.MarkerPath(beta.DataDir),
            JsonSerializer.Serialize(new { branch = "public", gameIdentityId = publicN.Id }),
            Encoding.UTF8
        );
        var save = WriteSaveSentinel(beta.DataDir);

        using (var update = BranchInstallUpdate.StartOrResume(
            beta.DataDir,
            beta.Branch,
            LifecycleDepots(1002)
        ))
        {
            ReplaceWithGeneration(beta, 1002, 0x46, "beta-source-generation-two");
            update.CompleteInstalledFiles("android-pck-v1");
        }

        True(publicPck.SequenceEqual(File.ReadAllBytes(publicInstall.PckPath)), "Updating beta must not change public PCK.");
        True(publicDll.SequenceEqual(File.ReadAllBytes(publicInstall.SourceAssemblyPath)), "Updating beta must not change public DLL.");
        True(publicState.SequenceEqual(File.ReadAllBytes(BranchInstallStateStore.PathFor(beta.DataDir, "public"))), "Updating beta must not rewrite public state.");
        True(publicPack.SequenceEqual(File.ReadAllBytes(publicPackManifest)), "Updating beta must not invalidate public runtime pack.");
        True(File.Exists(LauncherRuntimeSlotEvidence.MarkerPath(beta.DataDir)), "Output evidence for another active branch must be preserved.");
        True(BranchInstallStateStore.Current.TryReadReady(beta.DataDir, "public", publicN, out _, out _), "Public state must remain ready.");
        Equal("save-data", File.ReadAllText(save), "Branch-isolated update must not touch saves.");
    }

    private static void PublishInitialReady(
        GameInstallFixture fixture,
        GameIdentity identity,
        ulong manifestId
    )
    {
        var transactionId = Guid.NewGuid();
        var depots = LifecycleDepots(manifestId);
        BranchInstallStateStore.Current.BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            transactionId,
            "seeding-ready-state",
            depots
        );
        BranchInstallStateStore.Current.CommitReady(
            fixture.DataDir,
            fixture.Branch,
            transactionId,
            identity,
            depots,
            "android-pck-v1"
        );
    }

    private static BranchInstallDepot[] LifecycleDepots(ulong manifestId)
        => new[] { new BranchInstallDepot(123, manifestId, "selected") };

    private static void ReplaceWithGeneration(
        GameInstallFixture fixture,
        ulong manifestId,
        byte pckPayload,
        string sourceText
    )
    {
        fixture.WritePck(pckPayload);
        fixture.WriteSourceAssembly(sourceText);
        fixture.CompleteGeneration(manifestId);
    }

    private static void AssertUpdatingAndBlocked(GameInstallFixture fixture)
    {
        Equal(
            BranchInstallStatus.Updating,
            BranchInstallStateStore.Current.Read(fixture.DataDir, fixture.Branch).Status,
            "Interrupted update must remain updating."
        );
        True(
            !LauncherGameFiles.DownloadedForValidation(fixture.DataDir, fixture.Branch, out _),
            "Interrupted update must remain non-launchable."
        );
    }

    private static void AssertReadyFromCurrentFiles(GameInstallFixture fixture)
    {
        var identity = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        True(
            BranchInstallStateStore.Current.TryReadReady(
                fixture.DataDir,
                fixture.Branch,
                identity,
                out _,
                out var problem
            ),
            $"Recovered update must publish current files as ready: {problem}"
        );
    }

    private static string WriteActiveAssemblySentinel(string dataDir)
    {
        var path = Path.Combine(dataDir, ".godot", "mono", "publish", "arm64", "sts2.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "active-N", Encoding.UTF8);
        return path;
    }

    private static string WriteSaveSentinel(string dataDir)
    {
        var path = Path.Combine(dataDir, "saves", "profile.save");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "save-data", Encoding.UTF8);
        return path;
    }

    private sealed class SimulatedLifecycleInterruptionException : IOException
    {
    }
}
