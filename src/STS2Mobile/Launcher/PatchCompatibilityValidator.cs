using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static class PatchCompatibilityValidator
{
    private const string PatchSetVersion = "startup-orchestrator-v1";
    private const string ValidationMode = "static-critical-symbol-scan";
    private const string ValidationSurfaceVersion = "critical-startup-save-platform-model-v1";

    internal sealed class SymbolCheck
    {
        internal SymbolCheck(string category, string kind, string symbol, bool present)
        {
            Category = category;
            Kind = kind;
            Symbol = symbol;
            Present = present;
        }

        public string Category { get; }
        public string Kind { get; }
        public string Symbol { get; }
        public bool Present { get; }
        internal string FailureMessage => $"missing {Kind}: {Symbol}";
    }

    private sealed class RequiredCriticalSymbol
    {
        internal RequiredCriticalSymbol(string category, string kind, string symbol)
        {
            Category = category;
            Kind = kind;
            Symbol = symbol;
        }

        internal string Category { get; }
        internal string Kind { get; }
        internal string Symbol { get; }
    }

    private static readonly RequiredCriticalSymbol[] RequiredCriticalSymbols =
    {
        new("startup", "namespace", "MegaCrit.Sts2.Core.Nodes"),
        new("startup", "type", "GameStartupWrapper"),
        new("startup", "method", "StartOnMainMenu"),
        new("startup", "method", "InitializePlatform"),
        new("startup", "type", "NGame"),
        new("cloud-save", "namespace", "MegaCrit.Sts2.Core.Saves"),
        new("cloud-save", "type", "SaveManager"),
        new("cloud-save", "method", "ConstructDefault"),
        new("cloud-save", "method", "SyncCloudToLocal"),
        new("cloud-save", "type", "CloudSaveStore"),
        new("model-db", "namespace", "MegaCrit.Sts2.Core.Models"),
        new("model-db", "type", "ModelDb"),
        new("model-db", "method", "Init"),
        new("model-db", "field", "AllAbstractModelSubtypes"),
        new("model-db", "method", "GetId"),
        new("model-db", "method", "Contains"),
        new("model-db", "field", "_contentById"),
        new("platform", "namespace", "MegaCrit.Sts2.Core.Platform"),
        new("platform", "type", "PlatformUtil"),
        new("platform", "property", "PrimaryPlatform"),
        new("platform", "method", "GetPlatformUtil"),
        new("platform", "namespace", "MegaCrit.Sts2.Core.Platform.Null"),
        new("platform", "type", "NullPlatformUtilStrategy"),
    };

    internal static PatchCompatibilityEvidence ValidateSelectedVersion(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        var slot = GameRuntimeSlot.Inspect(dataDir, branch);
        if (string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
            return slot.PatchCompatibility;

        var markerPath = Path.Combine(
            slot.GameDirectory ?? string.Empty,
            PatchCompatibilityEvidence.GameDirectoryMarkerFileName
        );
        var failures = new List<string>();
        if (!slot.SourceAssemblyExists)
            failures.Add("selected source sts2.dll is missing");
        if (!File.Exists(slot.PckPath))
            failures.Add("selected PCK is missing");
        if (!LauncherGameFiles.BranchMarkerReady(dataDir, branch))
            failures.Add("branch marker is missing or mismatched");

        var symbolChecks = Array.Empty<SymbolCheck>();
        if (failures.Count == 0)
        {
            symbolChecks = CheckSymbols(slot.SourceAssemblyPath, out var readFailure);
            if (!string.IsNullOrWhiteSpace(readFailure))
                failures.Add(readFailure);
            failures.AddRange(symbolChecks.Where(symbol => !symbol.Present).Select(symbol => symbol.FailureMessage));
        }

        var status = failures.Count == 0 ? "passed" : "failed";
        WriteMarker(markerPath, slot, status, failures, symbolChecks);
        if (failures.Count == 0)
        {
            RuntimePackWriter.WriteValidatedRuntimePack(
                slot,
                PatchSetVersion,
                ValidationMode,
                "Critical startup patch symbols were found in the selected source assembly.",
                symbolChecks
            );
        }
        else
        {
            RuntimePackWriter.DeleteRuntimePack(slot, "selected-version patch compatibility validation failed");
        }

        var validatedSlot = GameRuntimeSlot.Inspect(dataDir, branch);
        var evidence = PatchCompatibilityEvidence.Inspect(
            dataDir,
            branch,
            validatedSlot.GameDirectory,
            validatedSlot.PckSha256,
            validatedSlot.SourceAssemblySha256,
            validatedSlot.RuntimePack,
            validatedSlot.RuntimePackSlotIdMatches
        );
        PatchHelper.Log(
            $"[Launcher] Patch compatibility validation for '{branch}' {status}: "
            + (failures.Count == 0 ? "critical symbols present" : string.Join("; ", failures.Take(4)))
        );
        return evidence;
    }

    private static SymbolCheck[] CheckSymbols(string assemblyPath, out string readFailure)
    {
        readFailure = string.Empty;
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(assemblyPath);
        }
        catch (Exception ex)
        {
            readFailure = $"source sts2.dll could not be read: {ex.GetType().Name}";
            return Array.Empty<SymbolCheck>();
        }

        return RequiredCriticalSymbols
            .Select(symbol => new SymbolCheck(
                symbol.Category,
                symbol.Kind,
                symbol.Symbol,
                ContainsAscii(bytes, symbol.Symbol)
            ))
            .ToArray();
    }

    private static bool ContainsAscii(byte[] haystack, string needle)
    {
        if (haystack == null || haystack.Length == 0 || string.IsNullOrWhiteSpace(needle))
            return false;

        var pattern = Encoding.UTF8.GetBytes(needle);
        if (pattern.Length > haystack.Length)
            return false;

        for (var i = 0; i <= haystack.Length - pattern.Length; i++)
        {
            var matched = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (haystack[i + j] == pattern[j])
                    continue;

                matched = false;
                break;
            }

            if (matched)
                return true;
        }

        return false;
    }

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
