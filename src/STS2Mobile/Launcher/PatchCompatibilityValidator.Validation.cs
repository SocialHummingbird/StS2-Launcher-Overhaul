using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class PatchCompatibilityValidator
{
    internal static PatchCompatibilityValidationResult ValidateSelectedVersionSlot(
        string dataDir,
        GameIdentity gameIdentity,
        GameRuntimeSlot slot
    )
    {
        if (gameIdentity == null)
            throw new ArgumentNullException(nameof(gameIdentity));
        if (slot == null)
            throw new ArgumentNullException(nameof(slot));

        var branch = gameIdentity.Branch;
        if (slot.GameIdentity != gameIdentity)
        {
            var mismatch = "Runtime-slot inspection does not contain the authoritative GameIdentity supplied for validation.";
            PatchHelper.Log($"[Launcher] Patch compatibility validation for '{branch}' rejected: {mismatch}");
            return new PatchCompatibilityValidationResult(slot, null, mismatch);
        }
        if (CurrentIdentityHasUsableValidatedPack(slot, gameIdentity))
        {
            PatchHelper.Log(
                $"[Launcher] Patch compatibility validation for '{branch}' skipped: current runtime pack already passed validation."
            );
            return new PatchCompatibilityValidationResult(slot, null, string.Empty);
        }

        var failures = new List<string>();
        if (!slot.SourceAssemblyExists)
            failures.Add("selected source sts2.dll is missing");
        if (!File.Exists(slot.PckPath))
            failures.Add("selected PCK is missing");
        if (!BranchInstallStateStore.Current.TryReadReady(
                dataDir,
                branch,
                gameIdentity,
                out _,
                out var installStateProblem
            ))
        {
            failures.Add($"installation state is not ready: {installStateProblem}");
        }

        var symbolChecks = Array.Empty<SymbolCheck>();
        if (failures.Count == 0)
        {
            symbolChecks = CheckSymbols(slot.SourceAssemblyPath, out var readFailure);
            if (!string.IsNullOrWhiteSpace(readFailure))
                failures.Add(readFailure);
            failures.AddRange(symbolChecks.Where(symbol => !symbol.Present).Select(symbol => symbol.FailureMessage));
        }

        RuntimePackCandidate candidate = null;
        if (failures.Count == 0)
        {
            var generation = RuntimePackWriter.GenerateCandidate(
                dataDir,
                gameIdentity,
                PatchSetVersion,
                ValidationMode,
                "Critical startup patch symbols were found in the selected source assembly.",
                symbolChecks
            );
            if (!generation.Succeeded)
                failures.Add($"runtime pack candidate generation failed: {generation.Problem}");
            else
                candidate = generation.Candidate;
        }

        var status = failures.Count == 0 ? "candidate generated" : "failed";
        LauncherLaunchReadinessCache.Clear($"patch compatibility validation updated runtime evidence for {branch}");
        var validatedSlot = GameRuntimeSlot.RefreshDerivedEvidence(dataDir, slot);
        PatchHelper.Log(
            $"[Launcher] Patch compatibility validation for '{branch}' {status}: "
            + (failures.Count == 0
                ? $"critical symbols present; staging={candidate.StagingDirectory}; promotion pending"
                : string.Join("; ", failures.Take(4)))
        );
        return new PatchCompatibilityValidationResult(
            validatedSlot,
            candidate,
            failures.Count == 0 ? string.Empty : string.Join("; ", failures)
        );
    }

    private static bool CurrentIdentityHasUsableValidatedPack(
        GameRuntimeSlot slot,
        GameIdentity gameIdentity
    )
        => slot?.GameIdentity != null
            && slot.GameIdentity == gameIdentity
            && slot.Playable
            && slot.RuntimePackUsable
            && slot.RuntimePack.SourceGameIdentity == gameIdentity
            && slot.RuntimePack?.PatchValidationPassed == true
            && slot.PatchCompatibility?.Passed == true;
}
