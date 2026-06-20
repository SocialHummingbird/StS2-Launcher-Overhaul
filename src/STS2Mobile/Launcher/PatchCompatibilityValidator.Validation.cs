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
}
