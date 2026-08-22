using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using STS2Mobile.Launcher;

namespace STS2Mobile.GameIdentityTests;

internal static partial class Program
{
    private static void RuntimePackCandidateGeneration()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);

        var generation = GenerateRuntimePackCandidate(fixture, identity);

        True(generation.Succeeded, $"Correct candidate generation must succeed: {generation.Problem}");
        var candidate = generation.Candidate;
        True(Directory.Exists(candidate.StagingDirectory), "A successful candidate must remain in staging for later promotion.");
        True(File.Exists(candidate.AndroidAssemblyPath), "The staged patched assembly must exist.");
        True(File.Exists(candidate.CompatibilityManifestPath), "The staged compatibility manifest must exist.");
        True(File.Exists(candidate.PatchValidationReportPath), "The staged validation report must exist.");
        True(
            candidate.StagingDirectory.StartsWith(
                GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch) + ".staging.",
                PathComparison()
            ),
            "Candidate staging must be a sibling of the unchanged final runtime-pack directory."
        );
        True(
            !Directory.Exists(GameRuntimeSlot.RuntimePackDirectoryPath(fixture.DataDir, fixture.Branch)),
            "Candidate generation must not create or promote the final runtime-pack directory."
        );
        Equal(identity, candidate.GameIdentity, "The candidate must carry the exact authoritative identity object.");
        Equal(
            HashFileForCandidateTest(candidate.AndroidAssemblyPath),
            candidate.Manifest.AndroidAssemblySha256,
            "The manifest must declare the actual staged patched-assembly hash."
        );
        AssertDeclaredIdentity(candidate.CompatibilityManifestPath, identity);
        AssertDeclaredIdentity(candidate.PatchValidationReportPath, identity);
        True(
            RuntimePackCandidateValidator.Validate(
                candidate.StagingDirectory,
                identity,
                PatchCompatibilityValidator.PatchSetVersion,
                PatchCompatibilityValidator.ValidationMode,
                PatchCompatibilityValidator.ValidationSurfaceVersion
            ).Usable,
            "A returned candidate must pass an independent read-back validation."
        );
    }

    private static void RuntimePackCandidateRejectsStaleIdentity()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(
            fixture.DataDir,
            fixture.Branch
        );
        Directory.CreateDirectory(finalDirectory);
        var finalSentinel = Path.Combine(finalDirectory, "existing-final.sentinel");
        File.WriteAllText(finalSentinel, "final-N");

        fixture.WritePck(0x62);
        fixture.CompleteGeneration(1002);
        var generation = GenerateRuntimePackCandidate(fixture, identity);

        True(!generation.Succeeded, "A stale supplied identity must reject candidate generation.");
        Contains(generation.Problem, "stale", "The rejection should identify the stale authoritative identity.");
        Equal("final-N", File.ReadAllText(finalSentinel), "Stale-input rejection must not alter an existing final pack.");
    }

    private static void RuntimePackCandidateRejectsChangingSource()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var generation = GenerateRuntimePackCandidate(
            fixture,
            identity,
            new RuntimePackGenerationHooks
            {
                AfterSourceCopied = _ => File.AppendAllText(
                    fixture.SourceAssemblyPath,
                    "source-changed-during-generation"
                ),
            }
        );

        True(!generation.Succeeded, "A source file changing during generation must reject the candidate.");
        True(generation.Candidate == null, "A changing source must not return a promotable candidate.");
    }

    private static void RuntimePackCandidateRejectsDeclaredSourceHashes()
    {
        using (var pckFixture = CreateRuntimePackFixture())
        {
            var identity = PublishRuntimePackFixtureReady(pckFixture);
            var generation = GenerateRuntimePackCandidate(
                pckFixture,
                identity,
                new RuntimePackGenerationHooks
                {
                    BeforeCandidateValidation = staging => MutateJson(
                        Path.Combine(staging, "compatibility.json"),
                        root =>
                        {
                            root["sourcePckSha256"] = new string('0', 64);
                            root["gameIdentity"]!["pckSha256"] = new string('0', 64);
                        }
                    ),
                }
            );
            True(!generation.Succeeded, "A candidate declaring the wrong PCK hash must be rejected.");
            Contains(generation.Problem, "identity", "Wrong PCK declaration should fail complete identity validation.");
        }

        using (var dllFixture = CreateRuntimePackFixture())
        {
            var identity = PublishRuntimePackFixtureReady(dllFixture);
            var generation = GenerateRuntimePackCandidate(
                dllFixture,
                identity,
                new RuntimePackGenerationHooks
                {
                    BeforeCandidateValidation = staging => MutateJson(
                        Path.Combine(staging, "patch_validation.json"),
                        root =>
                        {
                            root["sourceAssemblySha256"] = new string('f', 64);
                            root["gameIdentity"]!["sourceAssemblySha256"] = new string('f', 64);
                        }
                    ),
                }
            );
            True(!generation.Succeeded, "A candidate declaring the wrong source DLL hash must be rejected.");
            Contains(generation.Problem, "report", "Wrong DLL declaration should fail manifest/report consistency validation.");
        }
    }

    private static void RuntimePackCandidateRejectsPatchedAssemblyHash()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var wrongHash = new string('a', 64);
        var generation = GenerateRuntimePackCandidate(
            fixture,
            identity,
            new RuntimePackGenerationHooks
            {
                BeforeCandidateValidation = staging =>
                {
                    MutateJson(
                        Path.Combine(staging, "compatibility.json"),
                        root => root["androidAssemblySha256"] = wrongHash
                    );
                    MutateJson(
                        Path.Combine(staging, "patch_validation.json"),
                        root => root["androidAssemblySha256"] = wrongHash
                    );
                },
            }
        );

        True(!generation.Succeeded, "A false staged patched-assembly hash must reject the candidate.");
        Contains(generation.Problem, "hash mismatch", "The rejection should identify the actual assembly hash mismatch.");
    }

    private static void RuntimePackCandidateRejectsMissingOrCorruptReport()
    {
        using (var missingFixture = CreateRuntimePackFixture())
        {
            var identity = PublishRuntimePackFixtureReady(missingFixture);
            var generation = GenerateRuntimePackCandidate(
                missingFixture,
                identity,
                new RuntimePackGenerationHooks
                {
                    BeforeCandidateValidation = staging => File.Delete(
                        Path.Combine(staging, "patch_validation.json")
                    ),
                }
            );
            True(!generation.Succeeded, "A missing validation report must reject the candidate.");
            Contains(generation.Problem, "report is missing", "Missing-report diagnostics should be explicit.");
        }

        using (var corruptFixture = CreateRuntimePackFixture())
        {
            var identity = PublishRuntimePackFixtureReady(corruptFixture);
            var generation = GenerateRuntimePackCandidate(
                corruptFixture,
                identity,
                new RuntimePackGenerationHooks
                {
                    BeforeCandidateValidation = staging => File.WriteAllText(
                        Path.Combine(staging, "patch_validation.json"),
                        "{truncated"
                    ),
                }
            );
            True(!generation.Succeeded, "A corrupt validation report must reject the candidate.");
            Contains(generation.Problem, "read-back validation", "Corrupt-report diagnostics should identify read-back validation.");
        }
    }

    private static void RuntimePackCandidateRejectsInterruptedStaging()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(
            fixture.DataDir,
            fixture.Branch
        );
        var interrupted = finalDirectory + ".staging.interrupted";
        Directory.CreateDirectory(interrupted);
        File.Copy(
            fixture.SourceAssemblyPath,
            Path.Combine(interrupted, "sts2.dll")
        );
        File.WriteAllText(
            Path.Combine(interrupted, "patch_validation.json"),
            "{\"schemaVersion\":3}"
        );

        var validation = RuntimePackCandidateValidator.Validate(interrupted, identity);

        True(!validation.Usable, "An incomplete staging directory from an interrupted process must be rejected.");
        Contains(validation.Problem, "manifest is missing", "Interrupted staging should fail on its missing completed manifest.");
        var fresh = GenerateRuntimePackCandidate(fixture, identity);
        True(fresh.Succeeded, $"A fresh unique attempt should not reuse interrupted staging: {fresh.Problem}");
        NotEqual(
            Path.GetFullPath(interrupted),
            fresh.Candidate.StagingDirectory,
            "A fresh candidate must use a new attempt-specific staging directory."
        );
        True(Directory.Exists(interrupted), "Candidate generation must not silently adopt or mutate abandoned staging.");
    }

    private static void RuntimePackCandidateFailurePreservesActiveCache()
    {
        using var fixture = CreateRuntimePackFixture();
        var identity = PublishRuntimePackFixtureReady(fixture);
        var activePath = Path.Combine(
            fixture.DataDir,
            ".godot",
            "mono",
            "publish",
            "arm64",
            "sts2.dll"
        );
        Directory.CreateDirectory(Path.GetDirectoryName(activePath)!);
        File.Copy(typeof(Program).Assembly.Location, activePath, overwrite: true);
        var activeBefore = HashFileForCandidateTest(activePath);

        var generation = GenerateRuntimePackCandidate(
            fixture,
            identity,
            new RuntimePackGenerationHooks
            {
                AfterReportWritten = _ => throw new SimulatedCandidateInterruptionException(),
            }
        );

        True(!generation.Succeeded, "An interrupted generation must not return a candidate.");
        Equal(activeBefore, HashFileForCandidateTest(activePath), "Candidate failure must leave the active assembly cache byte-for-byte unchanged.");
    }

    private static GameInstallFixture CreateRuntimePackFixture()
    {
        var fixture = GameInstallFixture.Create();
        File.Copy(typeof(Program).Assembly.Location, fixture.SourceAssemblyPath, overwrite: true);
        File.SetLastWriteTimeUtc(fixture.SourceAssemblyPath, DateTime.UtcNow.AddMinutes(-2));
        fixture.CompleteGeneration(1001);
        return fixture;
    }

    private static GameIdentity PublishRuntimePackFixtureReady(
        GameInstallFixture fixture
    )
    {
        var identity = GameIdentityReader.ReadInstalled(fixture.DataDir, fixture.Branch);
        PublishInitialReady(fixture, identity, 1001);
        return identity;
    }

    private static RuntimePackCandidateGenerationResult GenerateRuntimePackCandidate(
        GameInstallFixture fixture,
        GameIdentity identity,
        RuntimePackGenerationHooks? hooks = null
    ) => RuntimePackWriter.GenerateCandidate(
        fixture.DataDir,
        identity,
        PatchCompatibilityValidator.PatchSetVersion,
        PatchCompatibilityValidator.ValidationMode,
        "Focused candidate-generation test.",
        new[]
        {
            new PatchCompatibilityValidator.SymbolCheck(
                "test",
                "type",
                nameof(Program),
                present: true
            ),
        },
        hooks
    );

    private static void AssertDeclaredIdentity(string path, GameIdentity identity)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        Equal(3, root.GetProperty("schemaVersion").GetInt32(), "Candidate outputs must use the staging schema.");
        Equal(identity.Id, root.GetProperty("gameIdentityId").GetString(), "Candidate output must declare the exact identity ID.");
        var declared = root.GetProperty("gameIdentity");
        Equal(identity.Branch, declared.GetProperty("branch").GetString(), "Candidate output must declare identity branch.");
        Equal(identity.InstallGeneration, declared.GetProperty("installGeneration").GetString(), "Candidate output must declare install generation.");
        Equal(identity.PckSha256, declared.GetProperty("pckSha256").GetString(), "Candidate output must declare PCK hash.");
        Equal(identity.SourceAssemblySha256, declared.GetProperty("sourceAssemblySha256").GetString(), "Candidate output must declare source DLL hash.");
    }

    private static void MutateJson(string path, Action<JsonObject> mutation)
    {
        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        mutation(root);
        File.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string HashFileForCandidateTest(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static StringComparison PathComparison()
        => OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    private sealed class SimulatedCandidateInterruptionException : IOException
    {
    }
}
