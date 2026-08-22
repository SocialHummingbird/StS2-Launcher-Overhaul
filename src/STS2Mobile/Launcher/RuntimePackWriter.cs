using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private const string RuntimeAssemblyFileName = "sts2.dll";
    private const string CompatibilityManifestFileName = "compatibility.json";
    private const string PatchValidationReportFileName = "patch_validation.json";

    internal static RuntimePackCandidateGenerationResult GenerateCandidate(
        string dataDir,
        GameIdentity gameIdentity,
        string patchSetVersion,
        string validationMode,
        string validationDetail,
        IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> symbolChecks,
        RuntimePackGenerationHooks hooks = null
    )
    {
        if (gameIdentity == null)
            return RuntimePackCandidateGenerationResult.Rejected(
                "An authoritative GameIdentity is required for runtime-pack generation."
            );

        string stagingDirectory = null;
        try
        {
            var readyState = RequireReadyState(dataDir, gameIdentity);
            RequireInstalledIdentity(dataDir, gameIdentity, "before runtime-pack generation");

            var paths = new RuntimePackGenerationPaths(dataDir, gameIdentity);
            var attemptId = Guid.NewGuid();
            stagingDirectory = CreateStagingDirectory(
                paths.FinalPackDirectory,
                readyState.TransactionId,
                attemptId
            );

            var runtimeAssembly = CopyRuntimeAssembly(
                paths.SourceAssemblyPath,
                paths.ActiveAndroidAssemblyPath,
                stagingDirectory,
                gameIdentity,
                hooks
            );
            var supportAssemblySha256 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var copiedSupportAssemblies = CopyRuntimeSupportAssemblies(
                paths.SourceAssemblyPath,
                stagingDirectory,
                supportAssemblySha256
            );
            var metadata = RuntimeSlotMetadata.Inspect(
                paths.ReleaseInfoPath,
                paths.BranchMarkerPath
            );
            var context = BuildRuntimePackWriteContext(
                gameIdentity,
                SteamGameBranch.DisplayName(gameIdentity.Branch),
                metadata,
                patchSetVersion,
                validationMode,
                symbolChecks,
                runtimeAssembly.Sha256,
                runtimeAssembly.PublicizerResult,
                copiedSupportAssemblies,
                supportAssemblySha256
            );

            WriteJson(
                Path.Combine(stagingDirectory, PatchValidationReportFileName),
                BuildPatchValidationReportPayload(context, validationDetail)
            );
            hooks?.AfterReportWritten?.Invoke(stagingDirectory);
            WriteJson(
                Path.Combine(stagingDirectory, CompatibilityManifestFileName),
                BuildCompatibilityManifestPayload(context)
            );
            hooks?.BeforeCandidateValidation?.Invoke(stagingDirectory);

            var validation = RuntimePackCandidateValidator.Validate(
                stagingDirectory,
                gameIdentity,
                patchSetVersion,
                validationMode,
                PatchCompatibilityValidator.ValidationSurfaceVersion
            );
            if (!validation.Usable)
            {
                throw new InvalidDataException(
                    $"Generated runtime-pack candidate failed read-back validation: {validation.Problem}"
                );
            }

            RequireInstalledIdentity(dataDir, gameIdentity, "after runtime-pack generation");
            var finalReadyState = RequireReadyState(dataDir, gameIdentity);
            if (finalReadyState.TransactionId != readyState.TransactionId)
            {
                throw new InvalidDataException(
                    "The ready installation transaction changed while the runtime-pack candidate was generated."
                );
            }

            var candidate = new RuntimePackCandidate(
                attemptId,
                readyState.TransactionId,
                gameIdentity,
                stagingDirectory,
                validation.Manifest
            );
            stagingDirectory = null;
            PatchHelper.Log(
                $"[Launcher] Generated validated runtime-pack candidate for '{gameIdentity.Branch}' identity={gameIdentity.Id} staging={candidate.StagingDirectory}"
            );
            return RuntimePackCandidateGenerationResult.Success(candidate);
        }
        catch (Exception ex)
        {
            DeleteOwnedStagingDirectory(stagingDirectory);
            var problem = ex.GetBaseException().Message;
            PatchHelper.Log(
                $"[Launcher] Runtime-pack candidate generation rejected for '{gameIdentity.Branch}' identity={gameIdentity.Id}: {problem}"
            );
            return RuntimePackCandidateGenerationResult.Rejected(problem);
        }
    }

    private static BranchInstallState RequireReadyState(
        string dataDir,
        GameIdentity gameIdentity
    ) => BranchInstallStateStore.Current.RequireReady(
        dataDir,
        gameIdentity.Branch,
        gameIdentity
    );

    private static void RequireInstalledIdentity(
        string dataDir,
        GameIdentity expected,
        string phase
    )
    {
        var actual = GameIdentityReader.ReadInstalled(dataDir, expected.Branch);
        if (actual != expected)
        {
            throw new InvalidDataException(
                $"Authoritative GameIdentity became stale {phase}: expected={expected.Id}; actual={actual.Id}."
            );
        }
    }

    private static void WriteJson(string path, object payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            payload,
            new JsonSerializerOptions { WriteIndented = true }
        );
        new AtomicFileWriter().WriteAllBytes(path, bytes);
    }

    private static void DeleteOwnedStagingDirectory(string stagingDirectory)
    {
        if (string.IsNullOrWhiteSpace(stagingDirectory))
            return;

        try
        {
            if (Directory.Exists(stagingDirectory))
                Directory.Delete(stagingDirectory, recursive: true);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Launcher] Failed to remove rejected runtime-pack staging directory '{stagingDirectory}': {ex.Message}"
            );
        }
    }
}
