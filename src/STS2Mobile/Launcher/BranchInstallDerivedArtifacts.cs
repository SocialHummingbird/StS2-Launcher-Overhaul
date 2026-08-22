using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal static class BranchInstallDerivedArtifacts
{
    internal static void InvalidatePreviousIdentity(
        string dataDir,
        GameIdentity newIdentity
    )
    {
        if (newIdentity == null)
            throw new ArgumentNullException(nameof(newIdentity));

        var result = SelectedBranchRecovery.Execute(
            dataDir,
            newIdentity.Branch,
            SelectedBranchRecoveryMode.DerivedArtifacts
        );
        result.RequireSuccess();

        PatchHelper.Log(
            $"[Launcher] Invalidated previous derived runtime artifacts for branch '{newIdentity.Branch}' identity={newIdentity.Id}; active Android assembly cache preserved."
        );
    }
}
