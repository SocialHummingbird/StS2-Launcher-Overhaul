using System;
using System.Linq;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private static object BuildCompatibilityManifestPayload(RuntimePackWriteContext context)
        => new
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

    private static object BuildPatchValidationReportPayload(
        RuntimePackWriteContext context,
        string validationDetail
    )
        => new
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
            symbolChecks = context.SymbolChecks.Select(symbol => new
            {
                symbol.Category,
                symbol.Kind,
                symbol.Symbol,
                symbol.Present
            }).ToArray(),
            categorySummaries = context.CategorySummaries,
            generatedUtc = DateTime.UtcNow.ToString("O")
        };

    private static object GameIdentityPayload(GameIdentity identity)
        => new
        {
            schemaVersion = GameIdentity.SchemaVersion,
            branch = identity.Branch,
            installGeneration = identity.InstallGeneration,
            pckSha256 = identity.PckSha256,
            sourceAssemblySha256 = identity.SourceAssemblySha256,
        };

    private static object AndroidAssemblyCompatibilityPayload(AndroidAssemblyPublicizer.Result result)
        => new
        {
            visibilityPublicizer = result.Changed || string.Equals(result.Status, "already public", StringComparison.OrdinalIgnoreCase),
            status = result.Status,
            publicizedTypes = result.TypeCount,
            publicizedMethods = result.MethodCount,
            publicizedFields = result.FieldCount,
        };
}
