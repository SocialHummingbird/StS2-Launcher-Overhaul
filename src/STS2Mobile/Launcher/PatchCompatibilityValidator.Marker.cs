using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class PatchCompatibilityValidator
{
    private static void WriteMarker(
        string markerPath,
        GameRuntimeSlot slot,
        string status,
        IReadOnlyList<string> failures,
        IReadOnlyList<SymbolCheck> symbolChecks
    )
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(markerPath) ?? slot.GameDirectory);
            symbolChecks ??= Array.Empty<SymbolCheck>();
            var missingSymbols = symbolChecks.Where(symbol => !symbol.Present).ToArray();
            var payload = new
            {
                status,
                detail = failures.Count == 0
                    ? "Critical startup patch symbols were found in the selected source assembly."
                    : string.Join("; ", failures.Take(12)),
                validationMode = ValidationMode,
                validationSurfaceVersion = ValidationSurfaceVersion,
                utc = DateTime.UtcNow.ToString("O"),
                branch = slot.Branch,
                selectedVersion = slot.DisplayName,
                gameDirectory = slot.GameDirectory,
                pckSha256 = slot.PckSha256,
                sourceAssemblySha256 = slot.SourceAssemblySha256,
                sourceAssemblyPath = slot.SourceAssemblyPath,
                patchSetVersion = PatchSetVersion,
                requiredSymbolCount = RequiredCriticalSymbols.Length,
                checkedSymbolCount = symbolChecks.Count,
                presentSymbolCount = symbolChecks.Count(symbol => symbol.Present),
                missingSymbolCount = missingSymbols.Length,
                missingSymbols = missingSymbols.Select(symbol => symbol.FailureMessage).Concat(
                    failures.Where(failure => !failure.StartsWith("missing ", StringComparison.OrdinalIgnoreCase))
                ).ToArray(),
                symbolChecks = symbolChecks.Select(symbol => new
                {
                    symbol.Category,
                    symbol.Kind,
                    symbol.Symbol,
                    symbol.Present
                }).ToArray(),
                categorySummaries = symbolChecks
                    .GroupBy(symbol => symbol.Category)
                    .Select(group => new
                    {
                        category = group.Key,
                        checkedCount = group.Count(),
                        presentCount = group.Count(symbol => symbol.Present),
                        missingCount = group.Count(symbol => !symbol.Present)
                    })
                    .OrderBy(group => group.category)
                    .ToArray()
            };
            File.WriteAllText(
                markerPath,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write patch compatibility marker: {ex.Message}");
        }
    }
}
