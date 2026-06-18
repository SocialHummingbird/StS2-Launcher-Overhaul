using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class RuntimePackWriter
{
    private const string RuntimeAssemblyFileName = "sts2.dll";
    private const string CompatibilityManifestFileName = "compatibility.json";
    private const string PatchValidationReportFileName = "patch_validation.json";
    private static readonly string[] RuntimeSupportAssemblyFileNames =
    {
        "Steamworks.NET.dll",
        "Sentry.dll"
    };

    internal static bool WriteValidatedRuntimePack(
        GameRuntimeSlot slot,
        string patchSetVersion,
        string validationMode,
        string validationDetail,
        IReadOnlyList<PatchCompatibilityValidator.SymbolCheck> symbolChecks
    )
    {
        if (slot == null || !slot.SourceAssemblyExists)
            return false;

        try
        {
            var packDirectory = Path.GetDirectoryName(slot.RuntimePackManifestPath);
            if (string.IsNullOrWhiteSpace(packDirectory))
                return false;

            if (Directory.Exists(packDirectory))
                Directory.Delete(packDirectory, recursive: true);
            Directory.CreateDirectory(packDirectory);
            var runtimeAssemblyPath = Path.Combine(packDirectory, RuntimeAssemblyFileName);
            File.Copy(slot.SourceAssemblyPath, runtimeAssemblyPath, overwrite: true);
            var copiedSupportAssemblies = CopyRuntimeSupportAssemblies(slot, packDirectory);
            var supportAssemblySha256 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var packId = RuntimePackId(slot, patchSetVersion);
            var runtimeSlotId = GameRuntimeSlot.BuildRuntimePackSlotId(slot, patchSetVersion, packId);
            var runtimeSlotIdentity = GameRuntimeSlot.BuildRuntimePackSlotIdentity(slot, patchSetVersion, packId);
            symbolChecks ??= Array.Empty<PatchCompatibilityValidator.SymbolCheck>();
            var missingSymbols = symbolChecks.Where(symbol => !symbol.Present).ToArray();
            var categorySummaries = symbolChecks
                .GroupBy(symbol => symbol.Category)
                .Select(group => new
                {
                    category = group.Key,
                    checkedCount = group.Count(),
                    presentCount = group.Count(symbol => symbol.Present),
                    missingCount = group.Count(symbol => !symbol.Present)
                })
                .OrderBy(group => group.category)
                .ToArray();

            var manifestPayload = new
            {
                packId,
                sourceRuntimeSlotId = runtimeSlotId,
                sourceRuntimeSlotIdentity = runtimeSlotIdentity,
                sourceBranch = slot.Branch,
                releaseVersion = slot.Metadata.ReleaseVersion,
                releaseCommit = slot.Metadata.ReleaseCommit,
                releaseBuildId = slot.Metadata.ReleaseBuildId,
                depotManifestCount = slot.Metadata.DepotManifestCount,
                depotManifestFingerprint = slot.Metadata.DepotManifestFingerprint,
                sourcePckSha256 = slot.PckSha256,
                sourceAssemblySha256 = slot.SourceAssemblySha256,
                androidAssemblySha256 = slot.SourceAssemblySha256,
                androidAssemblyFile = RuntimeAssemblyFileName,
                supportAssemblies = copiedSupportAssemblies,
                supportAssemblySha256,
                patchSetVersion,
                patchValidationStatus = "passed",
                patchValidationReport = PatchValidationReportFileName,
                validationMode,
                validationSurfaceVersion = "critical-startup-save-platform-model-v1",
                checkedSymbolCount = symbolChecks.Count,
                presentSymbolCount = symbolChecks.Count(symbol => symbol.Present),
                missingSymbolCount = missingSymbols.Length,
                generatedFromCleanDirectory = true,
                generatedUtc = DateTime.UtcNow.ToString("O")
            };

            File.WriteAllText(
                Path.Combine(packDirectory, CompatibilityManifestFileName),
                JsonSerializer.Serialize(manifestPayload, new JsonSerializerOptions { WriteIndented = true })
            );

            var validationPayload = new
            {
                status = "passed",
                detail = validationDetail,
                validationMode,
                branch = slot.Branch,
                sourceRuntimeSlotId = runtimeSlotId,
                sourceRuntimeSlotIdentity = runtimeSlotIdentity,
                selectedVersion = slot.DisplayName,
                releaseVersion = slot.Metadata.ReleaseVersion,
                releaseCommit = slot.Metadata.ReleaseCommit,
                releaseBuildId = slot.Metadata.ReleaseBuildId,
                depotManifestCount = slot.Metadata.DepotManifestCount,
                depotManifestFingerprint = slot.Metadata.DepotManifestFingerprint,
                pckSha256 = slot.PckSha256,
                sourceAssemblySha256 = slot.SourceAssemblySha256,
                androidAssemblySha256 = slot.SourceAssemblySha256,
                supportAssemblies = copiedSupportAssemblies,
                supportAssemblySha256,
                patchSetVersion,
                runtimePackId = packId,
                validationSurfaceVersion = "critical-startup-save-platform-model-v1",
                checkedSymbolCount = symbolChecks.Count,
                presentSymbolCount = symbolChecks.Count(symbol => symbol.Present),
                missingSymbolCount = missingSymbols.Length,
                generatedFromCleanDirectory = true,
                missingSymbols = missingSymbols.Select(symbol => symbol.FailureMessage).ToArray(),
                symbolChecks = symbolChecks.Select(symbol => new
                {
                    symbol.Category,
                    symbol.Kind,
                    symbol.Symbol,
                    symbol.Present
                }).ToArray(),
                categorySummaries,
                generatedUtc = DateTime.UtcNow.ToString("O")
            };

            File.WriteAllText(
                Path.Combine(packDirectory, PatchValidationReportFileName),
                JsonSerializer.Serialize(validationPayload, new JsonSerializerOptions { WriteIndented = true })
            );

            PatchHelper.Log($"[Launcher] Wrote runtime pack for '{slot.Branch}' to {packDirectory}");
            return true;
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to write runtime pack for '{slot.Branch}': {ex.Message}");
            return false;
        }
    }

    internal static void DeleteRuntimePack(GameRuntimeSlot slot, string reason)
    {
        if (slot == null)
            return;

        try
        {
            var packDirectory = Path.GetDirectoryName(slot.RuntimePackManifestPath);
            if (string.IsNullOrWhiteSpace(packDirectory) || !Directory.Exists(packDirectory))
                return;

            Directory.Delete(packDirectory, recursive: true);
            PatchHelper.Log($"[Launcher] Deleted runtime pack for '{slot.Branch}' because {reason}.");
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Failed to delete runtime pack for '{slot.Branch}': {ex.Message}");
        }
    }

    private static string RuntimePackId(GameRuntimeSlot slot, string patchSetVersion)
    {
        var pck = ShortHash(slot.PckSha256);
        var asm = ShortHash(slot.SourceAssemblySha256);
        return $"{slot.Branch}-{pck}-{asm}-{patchSetVersion}";
    }

    private static string ShortHash(string value)
        => string.IsNullOrWhiteSpace(value) || value.StartsWith("<", StringComparison.Ordinal)
            ? "unknown"
            : value.Length <= 12
                ? value
                : value.Substring(0, 12);

    private static string[] CopyRuntimeSupportAssemblies(GameRuntimeSlot slot, string packDirectory)
    {
        // Android packages the support assemblies with the app. Runtime packs only swap the branch-specific game assembly.
        return Array.Empty<string>();
    }
}
