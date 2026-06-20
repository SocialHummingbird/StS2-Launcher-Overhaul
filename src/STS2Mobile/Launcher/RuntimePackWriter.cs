using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static partial class RuntimePackWriter
{
    private const string RuntimeAssemblyFileName = "sts2.dll";
    private const string CompatibilityManifestFileName = "compatibility.json";
    private const string PatchValidationReportFileName = "patch_validation.json";
    private const string ValidationSurfaceVersion = "critical-startup-save-platform-model-v1";

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
            var packDirectory = PreparePackDirectory(slot);
            if (packDirectory == null)
                return false;

            CopyRuntimeAssembly(slot, packDirectory);
            var copiedSupportAssemblies = CopyRuntimeSupportAssemblies(slot, packDirectory);
            var supportAssemblySha256 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var context = BuildRuntimePackWriteContext(
                slot,
                patchSetVersion,
                validationMode,
                symbolChecks,
                copiedSupportAssemblies,
                supportAssemblySha256
            );

            WriteJson(
                Path.Combine(packDirectory, CompatibilityManifestFileName),
                BuildCompatibilityManifestPayload(context)
            );
            WriteJson(
                Path.Combine(packDirectory, PatchValidationReportFileName),
                BuildPatchValidationReportPayload(context, validationDetail)
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

    private static void WriteJson(string path, object payload)
        => File.WriteAllText(
            path,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })
        );
}
