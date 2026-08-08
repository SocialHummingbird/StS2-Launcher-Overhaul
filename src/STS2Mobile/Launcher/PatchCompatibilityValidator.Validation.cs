using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class PatchCompatibilityValidator
{
    internal static PatchCompatibilityEvidence ValidateSelectedVersion(string dataDir, string branch)
        => ValidateSelectedVersionSlot(dataDir, branch).PatchCompatibility;

    internal static GameRuntimeSlot ValidateSelectedVersionSlot(string dataDir, string branch)
    {
        branch = SteamGameBranch.Normalize(branch);
        var slot = GameRuntimeSlot.Inspect(dataDir, branch);
        if (SelectedVersionSlotAlreadyValidated(slot))
        {
            PatchHelper.Log(
                $"[Launcher] Patch compatibility validation for '{branch}' skipped: current runtime pack already passed validation."
            );
            return slot;
        }

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

        if (failures.Count == 0)
        {
            var runtimePackWritten = RuntimePackWriter.WriteValidatedRuntimePack(
                slot,
                PatchSetVersion,
                ValidationMode,
                "Critical startup patch symbols were found in the selected source assembly.",
                symbolChecks
            );
            if (!runtimePackWritten)
                failures.Add("runtime pack generation failed after patch compatibility validation");
        }

        var status = failures.Count == 0 ? "passed" : "failed";
        if (failures.Count > 0)
        {
            RuntimePackWriter.DeleteRuntimePack(slot, "selected-version patch compatibility validation failed");
        }
        WriteMarker(markerPath, slot, status, failures, symbolChecks);

        LauncherLaunchReadinessCache.Clear($"patch compatibility validation updated runtime evidence for {branch}");
        var validatedSlot = GameRuntimeSlot.Inspect(dataDir, branch);
        PatchHelper.Log(
            $"[Launcher] Patch compatibility validation for '{branch}' {status}: "
            + (failures.Count == 0 ? "critical symbols present" : string.Join("; ", failures.Take(4)))
        );
        return validatedSlot;
    }

    private static bool SelectedVersionSlotAlreadyValidated(GameRuntimeSlot slot)
        => slot?.Playable == true
            && slot.RuntimePackUsable
            && slot.RuntimePack?.PatchValidationPassed == true
            && slot.PatchCompatibility?.Passed == true;
}
