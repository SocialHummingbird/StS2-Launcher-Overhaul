using System;
using System.Linq;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private static object BuildCompatibilityManifestPayload(RuntimePackWriteContext context)
        => new
        {
            packId = context.PackId,
            sourceRuntimeSlotId = context.RuntimeSlotId,
            sourceRuntimeSlotIdentity = context.RuntimeSlotIdentity,
            sourceBranch = context.Slot.Branch,
            releaseVersion = context.Slot.Metadata.ReleaseVersion,
            releaseCommit = context.Slot.Metadata.ReleaseCommit,
            releaseBuildId = context.Slot.Metadata.ReleaseBuildId,
            depotManifestCount = context.Slot.Metadata.DepotManifestCount,
            depotManifestFingerprint = context.Slot.Metadata.DepotManifestFingerprint,
            sourcePckSha256 = context.Slot.PckSha256,
            sourceAssemblySha256 = context.Slot.SourceAssemblySha256,
            androidAssemblySha256 = context.Slot.SourceAssemblySha256,
            androidAssemblyFile = RuntimeAssemblyFileName,
            supportAssemblies = context.SupportAssemblies,
            supportAssemblySha256 = context.SupportAssemblySha256,
            patchSetVersion = context.PatchSetVersion,
            patchValidationStatus = "passed",
            patchValidationReport = PatchValidationReportFileName,
            validationMode = context.ValidationMode,
            validationSurfaceVersion = ValidationSurfaceVersion,
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
            status = "passed",
            detail = validationDetail,
            validationMode = context.ValidationMode,
            branch = context.Slot.Branch,
            sourceRuntimeSlotId = context.RuntimeSlotId,
            sourceRuntimeSlotIdentity = context.RuntimeSlotIdentity,
            selectedVersion = context.Slot.DisplayName,
            releaseVersion = context.Slot.Metadata.ReleaseVersion,
            releaseCommit = context.Slot.Metadata.ReleaseCommit,
            releaseBuildId = context.Slot.Metadata.ReleaseBuildId,
            depotManifestCount = context.Slot.Metadata.DepotManifestCount,
            depotManifestFingerprint = context.Slot.Metadata.DepotManifestFingerprint,
            pckSha256 = context.Slot.PckSha256,
            sourceAssemblySha256 = context.Slot.SourceAssemblySha256,
            androidAssemblySha256 = context.Slot.SourceAssemblySha256,
            supportAssemblies = context.SupportAssemblies,
            supportAssemblySha256 = context.SupportAssemblySha256,
            patchSetVersion = context.PatchSetVersion,
            runtimePackId = context.PackId,
            validationSurfaceVersion = ValidationSurfaceVersion,
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
}
