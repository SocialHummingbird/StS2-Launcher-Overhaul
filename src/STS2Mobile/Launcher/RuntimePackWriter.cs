using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;
using System.Security.Cryptography;
using System.Linq;
using System.Text;

namespace STS2Mobile.Launcher;
internal static partial class RuntimePackWriter
{
    private const string RuntimeAssemblyFileName = "sts2.dll";
    private const string CompatibilityManifestFileName = "compatibility.json";
    private const string PatchValidationReportFileName = "patch_validation.json";
    internal static RuntimePackCandidateGenerationResult GenerateCandidate(string dataDir, GameIdentity gameIdentity, string patchSetVersion, string validationMode, string validationDetail, IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> symbolChecks, RuntimePackGenerationHooks hooks = null)
    {
        if (gameIdentity == null)
            return RuntimePackCandidateGenerationResult.Rejected("An authoritative GameIdentity is required for runtime-pack generation.");
        string stagingDirectory = null;
        try
        {
            var readyState = RequireReadyState(dataDir, gameIdentity);
            RequireInstalledIdentity(dataDir, gameIdentity, "before runtime-pack generation");
            var paths = new RuntimePackGenerationPaths(dataDir, gameIdentity);
            var attemptId = Guid.NewGuid();
            stagingDirectory = CreateStagingDirectory(paths.FinalPackDirectory, readyState.TransactionId, attemptId);
            var runtimeAssembly = CopyRuntimeAssembly(paths.SourceAssemblyPath, paths.ActiveAndroidAssemblyPath, stagingDirectory, gameIdentity, hooks);
            var supportAssemblySha256 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var copiedSupportAssemblies = CopyRuntimeSupportAssemblies(paths.SourceAssemblyPath, stagingDirectory, supportAssemblySha256);
            var metadata = RuntimeSlotMetadata.Inspect(paths.ReleaseInfoPath, paths.BranchMarkerPath);
            var context = BuildRuntimePackWriteContext(gameIdentity, SteamGameBranch.DisplayName(gameIdentity.Branch), metadata, patchSetVersion, validationMode, symbolChecks, runtimeAssembly.Sha256, runtimeAssembly.PublicizerResult, copiedSupportAssemblies, supportAssemblySha256);
            WriteJson(Path.Combine(stagingDirectory, PatchValidationReportFileName), BuildPatchValidationReportPayload(context, validationDetail));
            hooks?.AfterReportWritten?.Invoke(stagingDirectory);
            WriteJson(Path.Combine(stagingDirectory, CompatibilityManifestFileName), BuildCompatibilityManifestPayload(context));
            hooks?.BeforeCandidateValidation?.Invoke(stagingDirectory);
            var validation = RuntimePackCandidateValidator.Validate(stagingDirectory, gameIdentity, patchSetVersion, validationMode, PatchCompatibilityValidator.ValidationSurfaceVersion);
            if (!validation.Usable)
            {
                throw new InvalidDataException($"Generated runtime-pack candidate failed read-back validation: {validation.Problem}");
            }

            RequireInstalledIdentity(dataDir, gameIdentity, "after runtime-pack generation");
            var finalReadyState = RequireReadyState(dataDir, gameIdentity);
            if (finalReadyState.TransactionId != readyState.TransactionId)
            {
                throw new InvalidDataException("The ready installation transaction changed while the runtime-pack candidate was generated.");
            }

            var candidate = new RuntimePackCandidate(attemptId, readyState.TransactionId, gameIdentity, stagingDirectory, validation.Manifest);
            stagingDirectory = null;
            PatchHelper.Log($"[Launcher] Generated validated runtime-pack candidate for '{gameIdentity.Branch}' identity={gameIdentity.Id} staging={candidate.StagingDirectory}");
            return RuntimePackCandidateGenerationResult.Success(candidate);
        }
        catch (Exception ex)
        {
            DeleteOwnedStagingDirectory(stagingDirectory);
            var problem = ex.GetBaseException().Message;
            PatchHelper.Log($"[Launcher] Runtime-pack candidate generation rejected for '{gameIdentity.Branch}' identity={gameIdentity.Id}: {problem}");
            return RuntimePackCandidateGenerationResult.Rejected(problem);
        }
    }

    private static BranchInstallState RequireReadyState(string dataDir, GameIdentity gameIdentity) => BranchInstallStateStore.Current.RequireReady(dataDir, gameIdentity.Branch, gameIdentity);
    private static void RequireInstalledIdentity(string dataDir, GameIdentity expected, string phase)
    {
        var actual = GameIdentityReader.ReadInstalled(dataDir, expected.Branch);
        if (actual != expected)
        {
            throw new InvalidDataException($"Authoritative GameIdentity became stale {phase}: expected={expected.Id}; actual={actual.Id}.");
        }
    }

    private static void WriteJson(string path, object payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions { WriteIndented = true });
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
            PatchHelper.Log($"[Launcher] Failed to remove rejected runtime-pack staging directory '{stagingDirectory}': {ex.Message}");
        }
    }

    private static readonly string[] RuntimeSupportAssemblyFileNames =
    {
    };
    private static string CreateStagingDirectory(string finalPackDirectory, Guid installTransactionId, Guid attemptId)
    {
        var parent = Path.GetDirectoryName(finalPackDirectory);
        if (string.IsNullOrWhiteSpace(parent))
            throw new IOException("Cannot resolve the runtime-pack parent directory.");
        Directory.CreateDirectory(parent);
        var stagingDirectory = Path.GetFullPath($"{finalPackDirectory}.staging.{installTransactionId:N}.{attemptId:N}");
        var parentPrefix = Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!stagingDirectory.StartsWith(parentPrefix, comparison))
            throw new IOException($"Refusing to stage a runtime pack outside its parent: {stagingDirectory}.");
        if (Directory.Exists(stagingDirectory) || File.Exists(stagingDirectory))
            throw new IOException($"Runtime-pack staging attempt already exists: {stagingDirectory}.");
        Directory.CreateDirectory(stagingDirectory);
        return stagingDirectory;
    }

    private static RuntimeAssemblyCopyResult CopyRuntimeAssembly(string sourceAssemblyPath, string activeAndroidAssemblyPath, string stagingDirectory, GameIdentity gameIdentity, RuntimePackGenerationHooks hooks)
    {
        var destinationPath = Path.Combine(stagingDirectory, RuntimeAssemblyFileName);
        File.Copy(sourceAssemblyPath, destinationPath, overwrite: false);
        var copiedSourceSha256 = GameIdentityFileHasher.Instance.Sha256(destinationPath);
        if (!string.Equals(copiedSourceSha256, gameIdentity.SourceAssemblySha256, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Copied source sts2.dll does not match the authoritative GameIdentity: expected={gameIdentity.SourceAssemblySha256}; actual={copiedSourceSha256}.");
        }

        hooks?.AfterSourceCopied?.Invoke(stagingDirectory);
        var publicizerResult = AndroidAssemblyPublicizer.Publicize(destinationPath, sourceAssemblyPath, activeAndroidAssemblyPath);
        return new RuntimeAssemblyCopyResult(destinationPath, GameIdentityFileHasher.Instance.Sha256(destinationPath), publicizerResult);
    }

    private static string[] CopyRuntimeSupportAssemblies(string sourceAssemblyPath, string stagingDirectory, IDictionary<string, string> supportAssemblySha256)
    {
        var sourceDirectory = Path.GetDirectoryName(sourceAssemblyPath);
        if (string.IsNullOrWhiteSpace(sourceDirectory) || !Directory.Exists(sourceDirectory))
            return Array.Empty<string>();
        var copied = new List<string>();
        foreach (var fileName in RuntimeSupportAssemblyFileNames)
        {
            var sourcePath = Path.Combine(sourceDirectory, fileName);
            if (!File.Exists(sourcePath))
                continue;
            var destinationPath = Path.Combine(stagingDirectory, fileName);
            File.Copy(sourcePath, destinationPath, overwrite: false);
            copied.Add(fileName);
            supportAssemblySha256[fileName] = GameIdentityFileHasher.Instance.Sha256(destinationPath);
        }

        return copied.ToArray();
    }

    private readonly record struct RuntimeAssemblyCopyResult(string Path, string Sha256, AndroidAssemblyPublicizer.Result PublicizerResult);
    private readonly struct RuntimePackGenerationPaths
    {
        internal RuntimePackGenerationPaths(string dataDir, GameIdentity gameIdentity)
        {
            var gameDirectory = Steam.SteamGameInstallPaths.GameDirectory(dataDir, gameIdentity.Branch);
            SourceAssemblyPath = GameIdentityReader.ResolveInstalledSourceAssemblyPath(gameDirectory);
            ActiveAndroidAssemblyPath = GameRuntimeSlot.FindActiveAndroidAssemblyPath(dataDir);
            ReleaseInfoPath = Path.Combine(gameDirectory, "release_info.json");
            BranchMarkerPath = Steam.SteamGameInstallPaths.BranchMarkerPath(dataDir, gameIdentity.Branch);
            FinalPackDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(dataDir, gameIdentity.Branch);
        }

        internal string SourceAssemblyPath { get; }
        internal string ActiveAndroidAssemblyPath { get; }
        internal string ReleaseInfoPath { get; }
        internal string BranchMarkerPath { get; }
        internal string FinalPackDirectory { get; }
    }

    private readonly record struct RuntimePackWriteContext(GameIdentity GameIdentity, string DisplayName, RuntimeSlotMetadata Metadata, string PatchSetVersion, string ValidationMode, string PackId, IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> SymbolChecks, PatchCompatibilityValidator.SymbolCheck[] MissingSymbols, object[] CategorySummaries, int CheckedSymbolCount, int PresentSymbolCount, string AndroidAssemblySha256, AndroidAssemblyPublicizer.Result PublicizerResult, string[] SupportAssemblies, IReadOnlyDictionary<string, string> SupportAssemblySha256);
    private static RuntimePackWriteContext BuildRuntimePackWriteContext(GameIdentity gameIdentity, string displayName, RuntimeSlotMetadata metadata, string patchSetVersion, string validationMode, IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> symbolChecks, string androidAssemblySha256, AndroidAssemblyPublicizer.Result publicizerResult, string[] supportAssemblies, IReadOnlyDictionary<string, string> supportAssemblySha256)
    {
        symbolChecks ??= Array.Empty<PatchCompatibilityValidator.SymbolCheck>();
        var missingSymbols = symbolChecks.Where(symbol => !symbol.Present).ToArray();
        var checkedSymbolCount = symbolChecks.Count;
        var presentSymbolCount = symbolChecks.Count(symbol => symbol.Present);
        var packId = RuntimePackId(gameIdentity, patchSetVersion, PatchCompatibilityValidator.ValidationSurfaceVersion, androidAssemblySha256, supportAssemblySha256);
        return new RuntimePackWriteContext(gameIdentity, displayName, metadata, patchSetVersion, validationMode, packId, symbolChecks, missingSymbols, BuildCategorySummaries(symbolChecks), checkedSymbolCount, presentSymbolCount, androidAssemblySha256, publicizerResult, supportAssemblies, supportAssemblySha256);
    }

    private static object[] BuildCategorySummaries(IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> symbolChecks) => symbolChecks.GroupBy(symbol => symbol.Category).Select(group => new { category = group.Key, checkedCount = group.Count(), presentCount = group.Count(symbol => symbol.Present), missingCount = group.Count(symbol => !symbol.Present) }).OrderBy(group => group.category).Cast<object>().ToArray();
    internal static string RuntimePackId(GameIdentity gameIdentity, string patchSetVersion, string validationSurfaceVersion, string androidAssemblySha256, IReadOnlyDictionary<string, string> supportAssemblySha256)
    {
        if (gameIdentity == null)
            throw new ArgumentNullException(nameof(gameIdentity));
        var support = supportAssemblySha256 == null ? string.Empty : string.Join("\n", supportAssemblySha256.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"support={pair.Key.ToLowerInvariant()}:{pair.Value.ToLowerInvariant()}"));
        var canonical = string.Join("\n", "runtime-pack-v1", $"gameIdentityId={gameIdentity.Id}", $"patchSetVersion={patchSetVersion}", $"validationSurfaceVersion={validationSurfaceVersion}", $"androidAssemblySha256={androidAssemblySha256}", support);
        var hash = Convert.ToHexString(AndroidJavaCrypto.Sha256HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return $"{gameIdentity.Branch}-{ShortHash(hash)}";
    }

    private static string ShortHash(string value) => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", StringComparison.Ordinal) ? "unknown" : value.Length <= 12 ? value : value.Substring(0, 12);
    private static object BuildCompatibilityManifestPayload(RuntimePackWriteContext context) => new
    {
        schemaVersion = 3,
        packId = context.PackId,
        gameIdentity = GameIdentityPayload(context.GameIdentity),
        gameIdentityId = context.GameIdentity.Id,
        sourceBranch = context.GameIdentity.Branch,
        releaseVersion = context.Metadata.ReleaseVersion,
        releaseCommit = context.Metadata.ReleaseCommit,
        releaseBuildId = context.Metadata.ReleaseBuildId,
        depotManifestCount = context.Metadata.DepotManifestCount,
        depotManifestFingerprint = context.Metadata.DepotManifestFingerprint,
        installGeneration = context.GameIdentity.InstallGeneration,
        sourcePckSha256 = context.GameIdentity.PckSha256,
        sourceAssemblySha256 = context.GameIdentity.SourceAssemblySha256,
        androidAssemblySha256 = context.AndroidAssemblySha256,
        androidAssemblyFile = RuntimeAssemblyFileName,
        androidAssemblyCompatibility = AndroidAssemblyCompatibilityPayload(context.PublicizerResult),
        supportAssemblies = context.SupportAssemblies,
        supportAssemblySha256 = context.SupportAssemblySha256,
        patchSetVersion = context.PatchSetVersion,
        patchValidationStatus = "passed",
        patchValidationReport = PatchValidationReportFileName,
        validationMode = context.ValidationMode,
        validationSurfaceVersion = PatchCompatibilityValidator.ValidationSurfaceVersion,
        checkedSymbolCount = context.CheckedSymbolCount,
        presentSymbolCount = context.PresentSymbolCount,
        missingSymbolCount = context.MissingSymbols.Length,
        generatedFromCleanDirectory = true,
        generatedUtc = DateTime.UtcNow.ToString("O")
    };
    private static object BuildPatchValidationReportPayload(RuntimePackWriteContext context, string validationDetail) => new
    {
        schemaVersion = 3,
        status = "passed",
        detail = validationDetail,
        validationMode = context.ValidationMode,
        branch = context.GameIdentity.Branch,
        gameIdentity = GameIdentityPayload(context.GameIdentity),
        gameIdentityId = context.GameIdentity.Id,
        selectedVersion = context.DisplayName,
        releaseVersion = context.Metadata.ReleaseVersion,
        releaseCommit = context.Metadata.ReleaseCommit,
        releaseBuildId = context.Metadata.ReleaseBuildId,
        depotManifestCount = context.Metadata.DepotManifestCount,
        depotManifestFingerprint = context.Metadata.DepotManifestFingerprint,
        installGeneration = context.GameIdentity.InstallGeneration,
        pckSha256 = context.GameIdentity.PckSha256,
        sourceAssemblySha256 = context.GameIdentity.SourceAssemblySha256,
        androidAssemblySha256 = context.AndroidAssemblySha256,
        androidAssemblyCompatibility = AndroidAssemblyCompatibilityPayload(context.PublicizerResult),
        supportAssemblies = context.SupportAssemblies,
        supportAssemblySha256 = context.SupportAssemblySha256,
        patchSetVersion = context.PatchSetVersion,
        runtimePackId = context.PackId,
        validationSurfaceVersion = PatchCompatibilityValidator.ValidationSurfaceVersion,
        checkedSymbolCount = context.CheckedSymbolCount,
        presentSymbolCount = context.PresentSymbolCount,
        missingSymbolCount = context.MissingSymbols.Length,
        generatedFromCleanDirectory = true,
        missingSymbols = context.MissingSymbols.Select(symbol => symbol.FailureMessage).ToArray(),
        symbolChecks = context.SymbolChecks.Select(symbol => new { symbol.Category, symbol.Kind, symbol.Symbol, symbol.Present }).ToArray(),
        categorySummaries = context.CategorySummaries,
        generatedUtc = DateTime.UtcNow.ToString("O")
    };
    private static object GameIdentityPayload(GameIdentity identity) => new
    {
        schemaVersion = GameIdentity.SchemaVersion,
        branch = identity.Branch,
        installGeneration = identity.InstallGeneration,
        pckSha256 = identity.PckSha256,
        sourceAssemblySha256 = identity.SourceAssemblySha256,
    };
    private static object AndroidAssemblyCompatibilityPayload(AndroidAssemblyPublicizer.Result result) => new
    {
        visibilityPublicizer = result.Changed || string.Equals(result.Status, "already public", StringComparison.OrdinalIgnoreCase),
        status = result.Status,
        publicizedTypes = result.TypeCount,
        publicizedMethods = result.MethodCount,
        publicizedFields = result.FieldCount,
    };
}
