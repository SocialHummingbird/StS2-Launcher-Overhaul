using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void Issue38CompleteLifecycle()
    {
        using var fixture = CreateRuntimePackFixture();
        var identityN = PublishRuntimePackFixtureReady(fixture);
        var cache = new TransactionalTestCachePreparer();
        var candidateN = GenerateRuntimePackCandidate(fixture, identityN);
        True(candidateN.Succeeded, candidateN.Problem);
        var launchN = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identityN,
            candidateN.Candidate,
            cache
        );
        True(launchN.Succeeded, launchN.Problem);

        var finalPack = GameRuntimeSlot.RuntimePackDirectoryPath(
            fixture.DataDir,
            fixture.Branch
        );
        var stalePackCopy = Path.Combine(
            Path.GetTempPath(),
            "sts2-issue-38-stale-pack",
            Guid.NewGuid().ToString("N")
        );
        CopyDirectory(finalPack, stalePackCopy);
        try
        {
            var staleAuthorization = File.ReadAllBytes(
                LauncherRuntimeSlotEvidence.MarkerPath(fixture.DataDir)
            );
            var stalePckCache = File.ReadAllBytes(
                GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch)
            );
            var legacyValidation = Path.Combine(
                fixture.GameDirectory,
                ".android_patch_validation.json"
            );
            var legacyPckMarker = Path.Combine(
                fixture.GameDirectory,
                ".android_pck_patch_v35"
            );
            File.WriteAllText(
                legacyValidation,
                JsonSerializer.Serialize(new
                {
                    branch = identityN.Branch,
                    pckSha256 = identityN.PckSha256,
                    sourceAssemblySha256 = identityN.SourceAssemblySha256,
                    gameIdentityId = identityN.Id,
                }),
                Encoding.UTF8
            );
            File.WriteAllText(
                legacyPckMarker,
                $"Source SHA-256: {identityN.PckSha256}\nPatched SHA-256: {identityN.PckSha256}\n",
                Encoding.UTF8
            );

            var savePath = Path.Combine(fixture.DataDir, "saves", "issue-38.save");
            var saveBytes = Enumerable.Range(0, 257)
                .Select(index => unchecked((byte)(index * 37)))
                .ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            File.WriteAllBytes(savePath, saveBytes);

            using var publicInstall = AddSiblingInstall(
                fixture.DataDir,
                "public",
                2001,
                0x31,
                "public-generation-one"
            );
            var publicIdentity = GameIdentityReader.ReadInstalled(
                publicInstall.DataDir,
                publicInstall.Branch
            );
            PublishInitialReady(publicInstall, publicIdentity, 2001);
            publicInstall.WriteMatchingRuntimePack();
            var publicPck = File.ReadAllBytes(publicInstall.PckPath);
            var publicSource = File.ReadAllBytes(publicInstall.SourceAssemblyPath);
            var publicState = File.ReadAllBytes(
                BranchInstallStateStore.PathFor(fixture.DataDir, "public")
            );
            var publicPack = DirectoryFingerprint(
                GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, "public")
            );

            var pckLength = new FileInfo(fixture.PckPath).Length;
            var pckMtime = File.GetLastWriteTimeUtc(fixture.PckPath);
            var dllLength = new FileInfo(fixture.SourceAssemblyPath).Length;
            var dllMtime = File.GetLastWriteTimeUtc(fixture.SourceAssemblyPath);

            BranchInstallCompletion completion;
            using (var update = BranchInstallUpdate.StartOrResume(
                fixture.DataDir,
                fixture.Branch,
                LifecycleDepots(1002)
            ))
            {
                Equal(
                    BranchInstallStatus.Updating,
                    update.State.Status,
                    "The production update entry point must revoke ready before mutation."
                );
                True(Directory.Exists(finalPack), "N runtime-pack evidence should still be present before completed-file invalidation.");
                True(File.Exists(legacyValidation), "The stale validation report should be present at the mutation boundary.");
                True(File.Exists(legacyPckMarker), "The stale PCK marker should be present at the mutation boundary.");

                update.RequireUpdatingBeforeInstalledMutation();
                WriteSameSizePckReplacement(fixture, 0x72, pckLength, pckMtime);
                WriteSameSizeAssemblyReplacement(fixture.SourceAssemblyPath, dllLength, dllMtime);
                fixture.CompleteGeneration(1002);

                var actualWhileUpdating = GameIdentityReader.ReadInstalled(
                    fixture.DataDir,
                    fixture.Branch
                );
                NotEqual(identityN, actualWhileUpdating, "Same-path, same-size, same-timestamp N+1 bytes must produce a new identity.");
                NotEqual(identityN.PckSha256, actualWhileUpdating.PckSha256, "The generation-bound PCK cache must not return N for N+1.");
                NotEqual(identityN.SourceAssemblySha256, actualWhileUpdating.SourceAssemblySha256, "The source DLL must be hashed directly despite identical metadata.");
                True(
                    !RuntimePackCandidateValidator.Validate(
                        finalPack,
                        actualWhileUpdating,
                        PatchCompatibilityValidator.PatchSetVersion,
                        PatchCompatibilityValidator.ValidationMode,
                        PatchCompatibilityValidator.ValidationSurfaceVersion
                    ).Usable,
                    "The N pack must be rejected against actual N+1 files while all N evidence remains present."
                );
                True(
                    !LauncherLaunchReadiness.Evaluate(
                        fixture.DataDir,
                        fixture.Branch,
                        "issue-38 updating boundary"
                    ).Ready,
                    "An updating branch must be non-launchable even with stale N authorization."
                );

                completion = update.CompleteInstalledFiles("android-pck-v1");
            }

            var identityNPlusOne = completion.GameIdentity;
            NotEqual(identityN, identityNPlusOne, "Completed N+1 must publish a distinct authoritative identity.");
            True(!File.Exists(legacyValidation), "Completed-file invalidation must remove the selected branch's legacy validation report.");
            True(!File.Exists(legacyPckMarker), "Completed-file invalidation must retire selected-branch PCK patch markers.");

            CopyDirectory(stalePackCopy, finalPack);
            File.WriteAllBytes(
                LauncherRuntimeSlotEvidence.MarkerPath(fixture.DataDir),
                staleAuthorization
            );
            File.WriteAllBytes(
                GameIdentityPckCache.PathFor(fixture.DataDir, fixture.Branch),
                stalePckCache
            );
            File.WriteAllText(legacyValidation, "{\"status\":\"passed\"}", Encoding.UTF8);
            File.WriteAllText(legacyPckMarker, identityN.PckSha256, Encoding.UTF8);

            var staleReadiness = LauncherLaunchReadiness.Evaluate(
                fixture.DataDir,
                fixture.Branch,
                "issue-38 restored stale evidence"
            );
            True(!staleReadiness.Ready, "Restored N reports, pack, PCK marker, cache, and runtime-slot evidence must not authorize N+1.");
            True(
                !LauncherRuntimeSlotEvidence.IsAuthorized(
                    fixture.DataDir,
                    identityNPlusOne,
                    launchN.RuntimeSlot.RuntimePack.PackId,
                    out _
                ),
                "The stale N runtime-slot marker must not authorize the N+1 identity."
            );

            var candidateNPlusOne = GenerateRuntimePackCandidate(
                fixture,
                identityNPlusOne
            );
            True(candidateNPlusOne.Succeeded, candidateNPlusOne.Problem);
            AssertDeclaredIdentity(
                candidateNPlusOne.Candidate.CompatibilityManifestPath,
                identityNPlusOne
            );
            AssertDeclaredIdentity(
                candidateNPlusOne.Candidate.PatchValidationReportPath,
                identityNPlusOne
            );

            var launchNPlusOne = RuntimePackLaunchLifecycle.Complete(
                fixture.DataDir,
                identityNPlusOne,
                candidateNPlusOne.Candidate,
                cache
            );
            True(launchNPlusOne.Succeeded, launchNPlusOne.Problem);
            AssertPromotedManifestContract(finalPack, identityNPlusOne);
            Equal(
                launchNPlusOne.RuntimeSlot.RuntimePack.AndroidAssemblySha256,
                HashFileForCandidateTest(cache.ActiveAssemblyPath),
                "The promoted active cache must contain the exact validated N+1 patched assembly."
            );

            var secondStartup = RuntimePackLaunchLifecycle.Complete(
                fixture.DataDir,
                identityNPlusOne,
                candidate: null,
                cache
            );
            True(secondStartup.Succeeded, $"A second startup must reuse N+1 without cleanup: {secondStartup.Problem}");
            True(!secondStartup.Promoted, "The second startup must not promote the final pack again.");

            True(saveBytes.SequenceEqual(File.ReadAllBytes(savePath)), "The complete N to N+1 lifecycle must preserve saves byte-for-byte.");
            True(publicPck.SequenceEqual(File.ReadAllBytes(publicInstall.PckPath)), "Updating public-beta must preserve public PCK bytes.");
            True(publicSource.SequenceEqual(File.ReadAllBytes(publicInstall.SourceAssemblyPath)), "Updating public-beta must preserve public source assembly bytes.");
            True(publicState.SequenceEqual(File.ReadAllBytes(BranchInstallStateStore.PathFor(fixture.DataDir, "public"))), "Updating public-beta must preserve public installation state.");
            Equal(publicPack, DirectoryFingerprint(GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, "public")), "Updating public-beta must preserve its sibling runtime pack.");
        }
        finally
        {
            TryDeleteTestDirectory(stalePackCopy);
        }
    }

    private static void Issue38MixedAndCorruptPacksFailClosed()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(fixture, identity);
        True(generation.Succeeded, generation.Problem);

        var reportPath = generation.Candidate.PatchValidationReportPath;
        var originalReport = File.ReadAllText(reportPath);
        MutateJson(reportPath, report =>
        {
            report["pckSha256"] = new string('0', 64);
            var declaredIdentity = report["gameIdentity"]!.AsObject();
            declaredIdentity["pckSha256"] = new string('0', 64);
        });
        var mixed = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            new RecordingCachePreparer()
        );
        True(!mixed.Succeeded, "A mixed-generation report and manifest must fail closed.");
        True(!LauncherRuntimeSlotEvidence.MarkerPresent(fixture.DataDir), "A mixed pack must not write launch authorization.");

        File.WriteAllText(reportPath, originalReport, Encoding.UTF8);
        File.WriteAllText(reportPath, "{\"schemaVersion\":3", Encoding.UTF8);
        var corrupt = RuntimePackLaunchLifecycle.Complete(
            fixture.DataDir,
            identity,
            generation.Candidate,
            new RecordingCachePreparer()
        );
        True(!corrupt.Succeeded, "A corrupt staged report must fail closed.");
        True(!Directory.Exists(GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch)), "A corrupt candidate must not become final.");
    }

    private static void WriteSameSizePckReplacement(
        GameInstallFixture fixture,
        byte payload,
        long expectedLength,
        DateTime expectedMtime
    )
    {
        fixture.WritePck(payload);
        Equal(expectedLength, new FileInfo(fixture.PckPath).Length, "The PCK fault injection requires identical length.");
        File.SetLastWriteTimeUtc(fixture.PckPath, expectedMtime);
    }

    private static void WriteSameSizeAssemblyReplacement(
        string path,
        long expectedLength,
        DateTime expectedMtime
    )
    {
        var bytes = File.ReadAllBytes(path);
        Equal(expectedLength, bytes.LongLength, "The DLL fault injection requires identical length.");
        if (bytes.Length < 80 || bytes[0] != (byte)'M' || bytes[1] != (byte)'Z')
            throw new InvalidDataException("The runtime-pack fixture source is not a mutable managed PE image.");

        // Offset 0x40 is in the ignored DOS stub of normal PE images. Changing
        // it changes the authoritative bytes without changing CLR metadata.
        bytes[0x40] ^= 0x01;
        File.WriteAllBytes(path, bytes);
        File.SetLastWriteTimeUtc(path, expectedMtime);
        Equal(expectedLength, new FileInfo(path).Length, "The replaced DLL must retain its exact length.");
        Equal(expectedMtime, File.GetLastWriteTimeUtc(path), "The replaced DLL must retain its exact timestamp.");
    }

    private static void AssertPromotedManifestContract(
        string finalPack,
        GameIdentity identity
    )
    {
        var manifestPath = Path.Combine(finalPack, "compatibility.json");
        var reportPath = Path.Combine(finalPack, "patch_validation.json");
        AssertDeclaredIdentity(manifestPath, identity);
        AssertDeclaredIdentity(reportPath, identity);

        using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        using var report = JsonDocument.Parse(File.ReadAllText(reportPath));
        var manifestRoot = manifest.RootElement;
        var reportRoot = report.RootElement;
        Equal(identity.Branch, manifestRoot.GetProperty("sourceBranch").GetString(), "Native manifest branch must be N+1.");
        Equal(identity.InstallGeneration, manifestRoot.GetProperty("installGeneration").GetString(), "Native manifest generation must be N+1.");
        Equal(identity.PckSha256, manifestRoot.GetProperty("sourcePckSha256").GetString(), "Native manifest PCK must be N+1.");
        Equal(identity.SourceAssemblySha256, manifestRoot.GetProperty("sourceAssemblySha256").GetString(), "Native manifest source DLL must be N+1.");
        Equal(identity.InstallGeneration, reportRoot.GetProperty("installGeneration").GetString(), "Validation report generation must be N+1.");
        Equal(identity.PckSha256, reportRoot.GetProperty("pckSha256").GetString(), "Validation report PCK must be N+1.");
        Equal(identity.SourceAssemblySha256, reportRoot.GetProperty("sourceAssemblySha256").GetString(), "Validation report source DLL must be N+1.");
        var declaredAndroidHash = manifestRoot.GetProperty("androidAssemblySha256").GetString();
        Equal(declaredAndroidHash, reportRoot.GetProperty("androidAssemblySha256").GetString(), "Manifest and report must declare one actual patched assembly hash.");
        Equal(declaredAndroidHash, HashFileForCandidateTest(Path.Combine(finalPack, "sts2.dll")), "The declared patched assembly hash must match promoted bytes.");
    }

    private static void CopyDirectory(string source, string destination)
    {
        if (Directory.Exists(destination))
            Directory.Delete(destination, recursive: true);
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    private static string DirectoryFingerprint(string directory)
    {
        var rows = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetRelativePath(directory, path), StringComparer.Ordinal)
            .Select(path => Path.GetRelativePath(directory, path).Replace('\\', '/') + ":" + HashFileForCandidateTest(path));
        return HashText(string.Join("\n", rows));
    }

    private static void TryDeleteTestDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }

    private sealed class TransactionalTestCachePreparer : IRuntimeAssemblyCachePreparer
    {
        internal string ActiveAssemblyPath { get; private set; } = string.Empty;

        public RuntimeAssemblyCachePreparationResult Prepare(
            string dataDir,
            GameIdentity gameIdentity,
            RuntimePackManifest manifest
        )
        {
            var validation = RuntimePackCandidateValidator.Validate(
                GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, gameIdentity.Branch),
                gameIdentity,
                PatchCompatibilityValidator.PatchSetVersion,
                PatchCompatibilityValidator.ValidationMode,
                PatchCompatibilityValidator.ValidationSurfaceVersion
            );
            if (!validation.Usable)
                return RuntimeAssemblyCachePreparationResult.Rejected(validation.Problem);

            ActiveAssemblyPath = Path.Combine(
                dataDir,
                ".godot",
                "mono",
                "publish",
                "arm64",
                "sts2.dll"
            );
            Directory.CreateDirectory(Path.GetDirectoryName(ActiveAssemblyPath)!);
            var staging = ActiveAssemblyPath + ".staging";
            var backup = ActiveAssemblyPath + ".backup";
            try
            {
                File.Copy(manifest.AndroidAssemblyPath, staging, overwrite: true);
                if (!string.Equals(HashFileForCandidateTest(staging), manifest.AndroidAssemblySha256, StringComparison.Ordinal))
                    throw new InvalidDataException("Staged active-cache assembly hash mismatch.");
                if (File.Exists(backup))
                    File.Delete(backup);
                if (File.Exists(ActiveAssemblyPath))
                    File.Move(ActiveAssemblyPath, backup);
                try
                {
                    File.Move(staging, ActiveAssemblyPath);
                }
                catch
                {
                    if (File.Exists(backup) && !File.Exists(ActiveAssemblyPath))
                        File.Move(backup, ActiveAssemblyPath);
                    throw;
                }
                if (File.Exists(backup))
                    File.Delete(backup);
                return RuntimeAssemblyCachePreparationResult.Success();
            }
            catch (Exception ex)
            {
                if (File.Exists(staging))
                    File.Delete(staging);
                return RuntimeAssemblyCachePreparationResult.Rejected(ex.GetBaseException().Message);
            }
        }
    }
}
