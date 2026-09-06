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
    private static void SelectedRecoveryClearsBetaAndPreservesPublic()
    {
        using var beta = GameInstallFixture.Create("public-beta");
        var publicInstall = AddSiblingInstall(
            beta.DataDir,
            "public",
            2001,
            0x31,
            "public-source"
        );
        var betaIdentity = SeedReadyInstall(beta, 1001);
        var publicIdentity = SeedReadyInstall(publicInstall, 2001);
        SeedDownloadState(beta);
        SeedDownloadState(publicInstall);
        var betaPack = SeedRuntimeArtifacts(beta.DataDir, beta.Branch);
        var publicPack = SeedRuntimeArtifacts(beta.DataDir, publicInstall.Branch);
        var publicSnapshot = SnapshotBranch(publicInstall, publicPack);
        var activeAssembly = WriteRecoveryActiveAssemblySentinel(beta.DataDir);
        var activeAssemblyBytes = File.ReadAllBytes(activeAssembly);
        WriteActiveCacheEvidence(beta.DataDir, publicIdentity);
        var activeEvidenceBytes = File.ReadAllBytes(
            LauncherRuntimeCacheEvidence.MarkerPath(beta.DataDir)
        );
        WriteRuntimeSlotEvidence(beta.DataDir, publicIdentity);
        var publicRuntimeSlotBytes = File.ReadAllBytes(
            LauncherRuntimeSlotEvidence.MarkerPath(beta.DataDir)
        );

        var result = SelectedBranchRecovery.Execute(
            beta.DataDir,
            "  PUBLIC-BETA ",
            SelectedBranchRecoveryMode.FullRedownload
        );

        True(result.Succeeded, string.Join(" | ", result.Failures));
        AssertBranchAbsent(beta);
        AssertBranchSnapshot(publicInstall, publicPack, publicSnapshot);
        True(
            BranchInstallStateStore.Current.TryReadReady(
                beta.DataDir,
                publicInstall.Branch,
                publicIdentity,
                out _,
                out _
            ),
            "Beta recovery must preserve public readiness."
        );
        True(
            activeAssemblyBytes.SequenceEqual(File.ReadAllBytes(activeAssembly)),
            "A valid active assembly cache belonging to public must remain byte-for-byte intact."
        );
        True(
            activeEvidenceBytes.SequenceEqual(
                File.ReadAllBytes(LauncherRuntimeCacheEvidence.MarkerPath(beta.DataDir))
            ),
            "Active-cache evidence belonging to public must be preserved."
        );
        True(
            publicRuntimeSlotBytes.SequenceEqual(
                File.ReadAllBytes(LauncherRuntimeSlotEvidence.MarkerPath(beta.DataDir))
            ),
            "Runtime-slot evidence belonging to public must be preserved."
        );
        Equal(
            betaIdentity.Branch,
            beta.Branch,
            "The seeded selected identity should belong to beta."
        );
    }

    private static void SelectedRecoveryClearsPublicAndPreservesBeta()
    {
        using var beta = GameInstallFixture.Create("public-beta");
        var publicInstall = AddSiblingInstall(
            beta.DataDir,
            "public",
            2001,
            0x32,
            "public-source"
        );
        var betaIdentity = SeedReadyInstall(beta, 1001);
        var publicIdentity = SeedReadyInstall(publicInstall, 2001);
        SeedDownloadState(beta);
        SeedDownloadState(publicInstall);
        var betaPack = SeedRuntimeArtifacts(beta.DataDir, beta.Branch);
        var publicPack = SeedRuntimeArtifacts(beta.DataDir, publicInstall.Branch);
        var betaSnapshot = SnapshotBranch(beta, betaPack);
        WriteRuntimeSlotEvidence(beta.DataDir, publicIdentity);
        WritePatchValidationEvidence(beta.DataDir, publicIdentity);
        WriteActiveCacheEvidence(beta.DataDir, publicIdentity);
        var activeAssembly = WriteRecoveryActiveAssemblySentinel(beta.DataDir);
        var activeAssemblyBytes = File.ReadAllBytes(activeAssembly);

        var result = SelectedBranchRecovery.Execute(
            beta.DataDir,
            publicInstall.Branch,
            SelectedBranchRecoveryMode.FullRedownload
        );

        True(result.Succeeded, string.Join(" | ", result.Failures));
        AssertBranchAbsent(publicInstall);
        AssertBranchSnapshot(beta, betaPack, betaSnapshot);
        True(
            BranchInstallStateStore.Current.TryReadReady(
                beta.DataDir,
                beta.Branch,
                betaIdentity,
                out _,
                out _
            ),
            "Public recovery must preserve beta readiness."
        );
        True(!Directory.Exists(publicPack), "The public runtime pack must be removed.");
        True(
            !LauncherRuntimeSlotEvidence.MarkerPresent(beta.DataDir),
            "Selected public runtime-slot evidence must be removed."
        );
        True(
            !LauncherRuntimePatchValidationEvidence.MarkerPresent(beta.DataDir),
            "Selected public validation evidence must be removed."
        );
        True(
            !LauncherRuntimeCacheEvidence.MarkerPresent(beta.DataDir),
            "Selected public active-cache evidence must be removed."
        );
        True(
            activeAssemblyBytes.SequenceEqual(File.ReadAllBytes(activeAssembly)),
            "Managed recovery removes stale cache evidence, not the working assembly payload."
        );
    }

    private static void SelectedRecoveryPreservesSavesAndCredentials()
    {
        using var fixture = GameInstallFixture.Create("public-beta");
        SeedReadyInstall(fixture, 1001);
        var savePath = Path.Combine(fixture.DataDir, "saves", "profile.save");
        var credentialsPath = Path.Combine(fixture.DataDir, "steam_credentials.enc");
        var unrelatedPath = Path.Combine(fixture.DataDir, "launcher_preferences.cfg");
        var saveBytes = Enumerable.Range(0, 257).Select(x => (byte)(x % 251)).ToArray();
        var credentialBytes = Enumerable.Range(0, 193).Select(x => (byte)(255 - (x % 251))).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
        File.WriteAllBytes(savePath, saveBytes);
        File.WriteAllBytes(credentialsPath, credentialBytes);
        File.WriteAllText(unrelatedPath, "unrelated-state", Encoding.UTF8);

        LauncherGameFiles.DeleteDownloadedState(fixture.DataDir, fixture.Branch);

        True(saveBytes.SequenceEqual(File.ReadAllBytes(savePath)), "Saves must remain byte-for-byte unchanged.");
        True(
            credentialBytes.SequenceEqual(File.ReadAllBytes(credentialsPath)),
            "Encrypted Steam credentials must remain byte-for-byte unchanged."
        );
        Equal(
            "unrelated-state",
            File.ReadAllText(unrelatedPath, Encoding.UTF8),
            "Unrelated launcher state must be preserved."
        );
    }

    private static void SelectedRecoveryIsIdempotent()
    {
        using var fixture = GameInstallFixture.Create("public-beta");
        SeedReadyInstall(fixture, 1001);
        SeedDownloadState(fixture);
        SeedRuntimeArtifacts(fixture.DataDir, fixture.Branch);

        var first = SelectedBranchRecovery.Execute(
            fixture.DataDir,
            fixture.Branch,
            SelectedBranchRecoveryMode.FullRedownload
        );
        var second = SelectedBranchRecovery.Execute(
            fixture.DataDir,
            fixture.Branch,
            SelectedBranchRecoveryMode.FullRedownload
        );

        True(first.Succeeded, string.Join(" | ", first.Failures));
        True(second.Succeeded, string.Join(" | ", second.Failures));
        AssertBranchAbsent(fixture);
        AssertNoRuntimeArtifacts(fixture.DataDir, fixture.Branch);
    }

    private static void SelectedRecoveryRemovesCorruptEvidence()
    {
        using var fixture = GameInstallFixture.Create("public-beta");
        SeedReadyInstall(fixture, 1001);
        File.WriteAllText(
            LauncherRuntimeSlotEvidence.MarkerPath(fixture.DataDir),
            "{ truncated",
            Encoding.UTF8
        );
        File.WriteAllText(
            LauncherRuntimePatchValidationEvidence.MarkerPath(fixture.DataDir),
            "[]",
            Encoding.UTF8
        );
        File.WriteAllText(
            LauncherRuntimeCacheEvidence.MarkerPath(fixture.DataDir),
            "cache marker without an owner",
            Encoding.UTF8
        );
        File.WriteAllText(
            Path.Combine(fixture.DataDir, "last_game_version_redownload.txt"),
            "corrupt legacy marker",
            Encoding.UTF8
        );
        var activeAssembly = WriteRecoveryActiveAssemblySentinel(fixture.DataDir);
        var activeBytes = File.ReadAllBytes(activeAssembly);

        var result = SelectedBranchRecovery.Execute(
            fixture.DataDir,
            fixture.Branch,
            SelectedBranchRecoveryMode.FullRedownload
        );

        True(result.Succeeded, string.Join(" | ", result.Failures));
        True(!LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "Corrupt runtime-slot evidence must be removed.");
        True(!LauncherRuntimePatchValidationEvidence.MarkerPresent(fixture.DataDir), "Corrupt validation evidence must be removed.");
        True(!LauncherRuntimeCacheEvidence.MarkerPresent(fixture.DataDir), "Corrupt active-cache evidence must be removed.");
        True(
            !File.Exists(Path.Combine(fixture.DataDir, "last_game_version_redownload.txt")),
            "Corrupt legacy cleanup evidence must be removed."
        );
        True(
            activeBytes.SequenceEqual(File.ReadAllBytes(activeAssembly)),
            "Corrupt cache evidence must not cause broad active-cache deletion."
        );
    }

    private static void SelectedRecoveryCompletesFromUpdatingState()
    {
        using var fixture = GameInstallFixture.Create("public-beta");
        BranchInstallStateStore.Current.BeginUpdating(
            fixture.DataDir,
            fixture.Branch,
            Guid.NewGuid(),
            "download-failed-after-replacement",
            LifecycleDepots(1002),
            "simulated interruption"
        );
        SeedDownloadState(fixture);

        var result = SelectedBranchRecovery.Execute(
            fixture.DataDir,
            fixture.Branch,
            SelectedBranchRecoveryMode.FullRedownload
        );

        True(result.Succeeded, string.Join(" | ", result.Failures));
        AssertBranchAbsent(fixture);
    }

    private static void SelectedRecoveryClearsInterruptedRuntimeGeneration()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var failed = GenerateRuntimePackCandidate(
            fixture,
            identity,
            new RuntimePackGenerationHooks
            {
                AfterReportWritten = _ => throw new IOException("simulated generation interruption"),
            }
        );
        True(!failed.Succeeded, "The setup must simulate a rejected runtime-pack generation.");
        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(
            fixture.DataDir,
            fixture.Branch
        );
        Directory.CreateDirectory(finalDirectory);
        Directory.CreateDirectory(finalDirectory + ".staging.interrupted");
        Directory.CreateDirectory(finalDirectory + ".backup.interrupted");
        Directory.CreateDirectory(finalDirectory + ".rejected.interrupted");
        File.WriteAllText(finalDirectory + ".promotion.lock", "", Encoding.UTF8);

        var result = SelectedBranchRecovery.Execute(
            fixture.DataDir,
            fixture.Branch,
            SelectedBranchRecoveryMode.FullRedownload
        );

        True(result.Succeeded, string.Join(" | ", result.Failures));
        AssertNoRuntimeArtifacts(fixture.DataDir, fixture.Branch);
        AssertBranchAbsent(fixture);
    }

    private static void SelectedRecoverySupportsRedownloadAndSecondLaunch()
    {
        using var fixture = CreateRuntimePackFixture();
        var identityN = PublishRuntimePackFixtureReady(fixture);
        var candidateN = GenerateRuntimePackCandidate(fixture, identityN);
        True(candidateN.Succeeded, candidateN.Problem);
        var firstLaunch = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identityN,
            candidateN.Candidate,
            new RecordingCachePreparer()
        );
        True(firstLaunch.Succeeded, firstLaunch.Problem);
        var savePath = WriteSaveSentinel(fixture.DataDir);
        var saveBytes = File.ReadAllBytes(savePath);

        LauncherGameFiles.DeleteDownloadedState(fixture.DataDir, fixture.Branch);
        True(
            !LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir),
            "Redownload must revoke selected launch authorization."
        );

        BranchInstallCompletion completion;
        using (var update = BranchInstallUpdate.StartOrResume(
            fixture.DataDir,
            fixture.Branch,
            LifecycleDepots(1002)
        ))
        {
            update.RequireUpdatingBeforeInstalledMutation();
            fixture.WritePck(0x62);
            Directory.CreateDirectory(Path.GetDirectoryName(fixture.SourceAssemblyPath)!);
            File.Copy(typeof(Program).Assembly.Location, fixture.SourceAssemblyPath, overwrite: true);
            File.SetLastWriteTimeUtc(fixture.SourceAssemblyPath, DateTime.UtcNow.AddMinutes(-2));
            fixture.CompleteGeneration(1002);
            completion = update.CompleteInstalledFiles(DepotDownloader.AndroidPckPreparationVersion);
        }

        var candidateNPlusOne = GenerateRuntimePackCandidate(
            fixture,
            completion.GameIdentity
        );
        True(candidateNPlusOne.Succeeded, candidateNPlusOne.Problem);
        var secondLaunch = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            completion.GameIdentity,
            candidateNPlusOne.Candidate,
            new RecordingCachePreparer()
        );

        True(secondLaunch.Succeeded, secondLaunch.Problem);
        NotEqual(identityN, completion.GameIdentity, "The redownload test must install a new identity.");
        True(
            saveBytes.SequenceEqual(File.ReadAllBytes(savePath)),
            "Redownload and second launch must preserve saves byte-for-byte."
        );
    }

    private static GameInstallFixture AddSiblingInstall(
        string dataDir,
        string branch,
        ulong manifest,
        byte pckPayload,
        string sourceText
    )
    {
        var fixture = new GameInstallFixture(dataDir, branch);
        fixture.WritePck(pckPayload);
        fixture.WriteSourceAssembly(sourceText);
        fixture.CompleteGeneration(manifest);
        return fixture;
    }

    private static GameIdentity SeedReadyInstall(
        GameInstallFixture fixture,
        ulong manifest
    )
    {
        var identity = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        PublishInitialReady(fixture, identity, manifest);
        return identity;
    }

    private static void SeedDownloadState(GameInstallFixture fixture)
    {
        var directory = SteamGameInstallPaths.DownloadStateDirectoryPath(
            fixture.DataDir,
            fixture.Branch
        );
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "123.id"), "download-state", Encoding.UTF8);
    }

    private static string SeedRuntimeArtifacts(string dataDir, string branch)
    {
        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch);
        Directory.CreateDirectory(finalDirectory);
        File.WriteAllText(
            Path.Combine(finalDirectory, "compatibility.json"),
            "runtime-pack",
            Encoding.UTF8
        );
        Directory.CreateDirectory(finalDirectory + ".staging.seed");
        Directory.CreateDirectory(finalDirectory + ".backup.seed");
        return finalDirectory;
    }

    private static byte[][] SnapshotBranch(
        GameInstallFixture fixture,
        string runtimePackDirectory
    )
        => new[]
        {
            File.ReadAllBytes(fixture.PckPath),
            File.ReadAllBytes(fixture.SourceAssemblyPath),
            File.ReadAllBytes(BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch)),
            File.ReadAllBytes(GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch)),
            File.ReadAllBytes(Path.Combine(runtimePackDirectory, "compatibility.json")),
            File.ReadAllBytes(
                Path.Combine(
                    SteamGameInstallPaths.DownloadStateDirectoryPath(
                        fixture.DataDir,
                        fixture.Branch
                    ),
                    "123.id"
                )
            ),
        };

    private static void AssertBranchSnapshot(
        GameInstallFixture fixture,
        string runtimePackDirectory,
        byte[][] expected
    )
    {
        var actual = SnapshotBranch(fixture, runtimePackDirectory);
        Equal(expected.Length, actual.Length, "Branch snapshot shape changed.");
        for (var i = 0; i < expected.Length; i++)
        {
            True(
                expected[i].SequenceEqual(actual[i]),
                $"Unrelated branch artifact {i} changed during selected recovery."
            );
        }
        True(
            Directory.Exists(runtimePackDirectory + ".staging.seed"),
            "Other-branch runtime staging must be preserved."
        );
        True(
            Directory.Exists(runtimePackDirectory + ".backup.seed"),
            "Other-branch runtime backup must be preserved."
        );
    }

    private static void AssertBranchAbsent(GameInstallFixture fixture)
    {
        True(!Directory.Exists(fixture.GameDirectory), "Selected game directory must be removed.");
        True(
            !Directory.Exists(
                SteamGameInstallPaths.DownloadStateDirectoryPath(
                    fixture.DataDir,
                    fixture.Branch
                )
            ),
            "Selected download state must be removed."
        );
        True(
            !File.Exists(BranchInstallStateStore.PathFor(fixture.DataDir, fixture.Branch)),
            "Selected ready/updating state must be removed last."
        );
        True(
            !File.Exists(GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch)),
            "Selected GameIdentity PCK cache must be removed."
        );
        AssertNoRuntimeArtifacts(fixture.DataDir, fixture.Branch);
    }

    private static void AssertNoRuntimeArtifacts(string dataDir, string branch)
    {
        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, branch);
        var parent = Path.GetDirectoryName(finalDirectory)!;
        if (!Directory.Exists(parent))
            return;
        var name = Path.GetFileName(finalDirectory);
        var owned = Directory.EnumerateFileSystemEntries(parent, name + "*")
            .Where(path =>
            {
                var artifact = Path.GetFileName(path);
                return string.Equals(artifact, name, StringComparison.Ordinal)
                    || artifact.StartsWith(name + ".staging.", StringComparison.Ordinal)
                    || artifact.StartsWith(name + ".backup.", StringComparison.Ordinal)
                    || artifact.StartsWith(name + ".rejected.", StringComparison.Ordinal)
                    || string.Equals(artifact, name + ".promotion.lock", StringComparison.Ordinal);
            })
            .ToArray();
        Equal(0, owned.Length, "All selected runtime-pack final/staging/backup evidence must be removed.");
    }

    private static string WriteRecoveryActiveAssemblySentinel(string dataDir)
    {
        var path = Path.Combine(dataDir, ".godot", "mono", "publish", "arm64", "sts2.dll");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "active-cache-payload", Encoding.UTF8);
        return path;
    }

    private static void WriteRuntimeSlotEvidence(string dataDir, GameIdentity identity)
        => File.WriteAllText(
            LauncherRuntimeSlotEvidence.MarkerPath(dataDir),
            JsonSerializer.Serialize(new { branch = identity.Branch, gameIdentityId = identity.Id }),
            Encoding.UTF8
        );

    private static void WritePatchValidationEvidence(string dataDir, GameIdentity identity)
        => File.WriteAllText(
            LauncherRuntimePatchValidationEvidence.MarkerPath(dataDir),
            JsonSerializer.Serialize(new { selectedBranch = identity.Branch, gameIdentityId = identity.Id }),
            Encoding.UTF8
        );

    private static void WriteActiveCacheEvidence(string dataDir, GameIdentity identity)
        => File.WriteAllText(
            LauncherRuntimeCacheEvidence.MarkerPath(dataDir),
            $"Active branch: {identity.Branch}{Environment.NewLine}Game identity ID: {identity.Id}{Environment.NewLine}",
            Encoding.UTF8
        );
}
