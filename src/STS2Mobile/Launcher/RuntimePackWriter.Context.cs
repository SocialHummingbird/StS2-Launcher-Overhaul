using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private readonly record struct RuntimePackWriteContext(
        GameIdentity GameIdentity,
        string DisplayName,
        RuntimeSlotMetadata Metadata,
        string PatchSetVersion,
        string ValidationMode,
        string PackId,
        IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> SymbolChecks,
        PatchCompatibilityValidator.SymbolCheck[] MissingSymbols,
        object[] CategorySummaries,
        int CheckedSymbolCount,
        int PresentSymbolCount,
        string AndroidAssemblySha256,
        AndroidAssemblyPublicizer.Result PublicizerResult,
        string[] SupportAssemblies,
        IReadOnlyDictionary<string, string> SupportAssemblySha256
    );

    private static RuntimePackWriteContext BuildRuntimePackWriteContext(
        GameIdentity gameIdentity,
        string displayName,
        RuntimeSlotMetadata metadata,
        string patchSetVersion,
        string validationMode,
        IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> symbolChecks,
        string androidAssemblySha256,
        AndroidAssemblyPublicizer.Result publicizerResult,
        string[] supportAssemblies,
        IReadOnlyDictionary<string, string> supportAssemblySha256
    )
    {
        symbolChecks ??= Array.Empty<PatchCompatibilityValidator.SymbolCheck>();
        var missingSymbols = symbolChecks.Where(symbol => !symbol.Present).ToArray();
        var checkedSymbolCount = symbolChecks.Count;
        var presentSymbolCount = symbolChecks.Count(symbol => symbol.Present);
        var packId = RuntimePackId(
            gameIdentity,
            patchSetVersion,
            PatchCompatibilityValidator.ValidationSurfaceVersion,
            androidAssemblySha256,
            supportAssemblySha256
        );

        return new RuntimePackWriteContext(
            gameIdentity,
            displayName,
            metadata,
            patchSetVersion,
            validationMode,
            packId,
            symbolChecks,
            missingSymbols,
            BuildCategorySummaries(symbolChecks),
            checkedSymbolCount,
            presentSymbolCount,
            androidAssemblySha256,
            publicizerResult,
            supportAssemblies,
            supportAssemblySha256
        );
    }

    private static object[] BuildCategorySummaries(
        IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> symbolChecks
    )
        => symbolChecks
            .GroupBy(symbol => symbol.Category)
            .Select(group => new
            {
                category = group.Key,
                checkedCount = group.Count(),
                presentCount = group.Count(symbol => symbol.Present),
                missingCount = group.Count(symbol => !symbol.Present)
            })
            .OrderBy(group => group.category)
            .Cast<object>()
            .ToArray();
}
