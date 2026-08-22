using System;
using System.IO;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class RuntimePackLaunchLifecycle
{
    internal static RuntimePackLaunchPreparationResult Complete(
        string dataDir,
        GameIdentity expectedIdentity,
        RuntimePackCandidate candidate,
        IRuntimeAssemblyCachePreparer cachePreparer = null,
        RuntimePackLaunchPreparationHooks hooks = null
    )
    {
        if (expectedIdentity == null)
        {
            return RuntimePackLaunchPreparationResult.Rejected(
                runtimeSlot: null,
                "An authoritative GameIdentity is required for launch preparation."
            );
        }

        GameRuntimeSlot slot = null;
        try
        {
            LauncherRuntimeSlotEvidence.Revoke(dataDir);
        }
        catch (Exception ex)
        {
            return RuntimePackLaunchPreparationResult.Rejected(
                runtimeSlot: null,
                $"Existing launch authorization could not be revoked before preparation: {ex.GetBaseException().Message}"
            );
        }
        try
        {
            var promotion = RuntimePackPromoter.PromoteOrValidate(
                dataDir,
                expectedIdentity,
                candidate,
                hooks?.Promotion
            );
            if (!promotion.Succeeded)
            {
                return RuntimePackLaunchPreparationResult.Rejected(
                    GameRuntimeSlot.Inspect(dataDir, expectedIdentity),
                    promotion.Problem
                );
            }

            RequireCurrentReadyIdentity(dataDir, expectedIdentity);
            var promotedValidation = RuntimePackCandidateValidator.Validate(
                promotion.FinalDirectory,
                expectedIdentity,
                PatchCompatibilityValidator.PatchSetVersion,
                PatchCompatibilityValidator.ValidationMode,
                PatchCompatibilityValidator.ValidationSurfaceVersion
            );
            if (!promotedValidation.Usable)
            {
                throw new InvalidDataException(
                    $"Promoted runtime pack failed launch-boundary validation: {promotedValidation.Problem}"
                );
            }

            slot = GameRuntimeSlot.Inspect(dataDir, expectedIdentity);
            if (!slot.Playable)
                throw new InvalidDataException(slot.ReadinessProblem() ?? "The promoted runtime pack is not playable.");

            var cacheResult = (cachePreparer ?? RuntimeAssemblyCachePreparer.Current).Prepare(
                dataDir,
                expectedIdentity,
                promotedValidation.Manifest
            );
            if (!cacheResult.Succeeded)
                throw new InvalidDataException(cacheResult.Problem);

            RequireCurrentReadyIdentity(dataDir, expectedIdentity);
            var finalValidation = RuntimePackCandidateValidator.Validate(
                promotion.FinalDirectory,
                expectedIdentity,
                PatchCompatibilityValidator.PatchSetVersion,
                PatchCompatibilityValidator.ValidationMode,
                PatchCompatibilityValidator.ValidationSurfaceVersion
            );
            if (!finalValidation.Usable
                || !string.Equals(
                    finalValidation.Manifest.PackId,
                    promotedValidation.Manifest.PackId,
                    StringComparison.Ordinal
                ))
            {
                throw new InvalidDataException(
                    "The promoted runtime pack changed while the active assembly cache was prepared."
                );
            }

            slot = GameRuntimeSlot.Inspect(dataDir, expectedIdentity);
            if (!slot.Playable
                || slot.RuntimePack.SourceGameIdentity != expectedIdentity
                || !string.Equals(
                    slot.RuntimePack.PackId,
                    finalValidation.Manifest.PackId,
                    StringComparison.Ordinal
                ))
            {
                throw new InvalidDataException(
                    "Post-cache runtime-slot inspection no longer matches the authorized GameIdentity and runtime pack."
                );
            }
            if (OperatingSystem.IsAndroid()
                && !slot.ActiveAndroidAssemblyMatchesPreparedRuntime)
            {
                throw new InvalidDataException(
                    "The native Android assembly-cache preparation returned success, but the active sts2.dll does not match the promoted runtime pack."
                );
            }

            hooks?.BeforeAuthorizationMarker?.Invoke();
            RequireCurrentReadyIdentity(dataDir, expectedIdentity);
            LauncherRuntimeSlotEvidence.WriteAuthorization(dataDir, slot);
            if (!LauncherRuntimeSlotEvidence.IsAuthorized(
                    dataDir,
                    expectedIdentity,
                    finalValidation.Manifest.PackId,
                    out var authorizationProblem
                ))
            {
                throw new InvalidDataException(authorizationProblem);
            }

            PatchHelper.Log(
                $"[Launcher] Launch authorized for '{expectedIdentity.Branch}' identity={expectedIdentity.Id} pack={finalValidation.Manifest.PackId}"
            );
            return RuntimePackLaunchPreparationResult.Success(slot, promotion.Promoted);
        }
        catch (Exception ex)
        {
            var problem = ex.GetBaseException().Message;
            try
            {
                LauncherRuntimeSlotEvidence.Revoke(dataDir);
            }
            catch (Exception revokeError)
            {
                problem += $" Launch authorization cleanup also failed: {revokeError.GetBaseException().Message}";
            }
            PatchHelper.Log(
                $"[Launcher] Runtime-pack launch preparation rejected for '{expectedIdentity.Branch}' identity={expectedIdentity.Id}: {problem}"
            );
            return RuntimePackLaunchPreparationResult.Rejected(slot, problem);
        }
    }

    internal static bool ReconfirmAuthorization(
        string dataDir,
        GameIdentity expectedIdentity,
        string expectedPackId,
        out string problem
    )
    {
        try
        {
            RequireCurrentReadyIdentity(dataDir, expectedIdentity);
            var finalDirectory = GameRuntimeSlot.RuntimePackDirectoryPath(
                dataDir,
                expectedIdentity.Branch
            );
            var validation = RuntimePackCandidateValidator.Validate(
                finalDirectory,
                expectedIdentity,
                PatchCompatibilityValidator.PatchSetVersion,
                PatchCompatibilityValidator.ValidationMode,
                PatchCompatibilityValidator.ValidationSurfaceVersion
            );
            if (!validation.Usable
                || !string.Equals(validation.Manifest.PackId, expectedPackId, StringComparison.Ordinal))
            {
                problem = validation.Usable
                    ? "The selected runtime-pack ID changed after launch authorization."
                    : $"The selected runtime pack is no longer valid: {validation.Problem}";
                return false;
            }

            return LauncherRuntimeSlotEvidence.IsAuthorized(
                dataDir,
                expectedIdentity,
                expectedPackId,
                out problem
            );
        }
        catch (Exception ex)
        {
            problem = $"Launch authorization is stale: {ex.GetBaseException().Message}";
            return false;
        }
    }

    private static void RequireCurrentReadyIdentity(
        string dataDir,
        GameIdentity expectedIdentity
    )
    {
        BranchInstallStateStore.Current.RequireReady(
            dataDir,
            expectedIdentity.Branch,
            expectedIdentity
        );
        var actual = GameIdentityReader.ReadInstalled(dataDir, expectedIdentity.Branch);
        if (actual != expectedIdentity)
        {
            throw new InvalidDataException(
                $"The selected branch GameIdentity changed during launch preparation: expected={expectedIdentity.Id}; actual={actual.Id}."
            );
        }
    }
}
