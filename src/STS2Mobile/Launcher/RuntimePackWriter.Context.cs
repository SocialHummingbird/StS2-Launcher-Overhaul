using System;
using System.Collections.Generic;
using System.Linq;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private readonly record struct RuntimePackWriteContext(
        GameRuntimeSlot Slot,
        string PatchSetVersion,
        string ValidationMode,
        string PackId,
        string RuntimeSlotId,
        string RuntimeSlotIdentity,
        IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> SymbolChecks,
        PatchCompatibilityValidator.SymbolCheck[] MissingSymbols,
        object[] CategorySummaries,
        int CheckedSymbolCount,
        int PresentSymbolCount,
        string[] SupportAssemblies,
        IReadOnlyDictionary<string, string> SupportAssemblySha256
    );

    private static RuntimePackWriteContext BuildRuntimePackWriteContext(
        GameRuntimeSlot slot,
        string patchSetVersion,
        string validationMode,
        IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> symbolChecks,
        string[] supportAssemblies,
        IReadOnlyDictionary<string, string> supportAssemblySha256
    )
    {
        symbolChecks ??= Array.Empty<PatchCompatibilityValidator.SymbolCheck>();
        var missingSymbols = symbolChecks.Where(symbol => !symbol.Present).ToArray();
        var checkedSymbolCount = symbolChecks.Count;
        var presentSymbolCount = symbolChecks.Count(symbol => symbol.Present);
        var packId = RuntimePackId(slot, patchSetVersion);

        return new RuntimePackWriteContext(
            slot,
            patchSetVersion,
            validationMode,
            packId,
            GameRuntimeSlot.BuildRuntimePackSlotId(slot, patchSetVersion, packId),
            GameRuntimeSlot.BuildRuntimePackSlotIdentity(slot, patchSetVersion, packId),
            symbolChecks,
            missingSymbols,
            BuildCategorySummaries(symbolChecks),
            checkedSymbolCount,
            presentSymbolCount,
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
