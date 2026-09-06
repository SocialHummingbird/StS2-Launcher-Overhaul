using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    internal bool RuntimePackUsable => RuntimePack?.Usable == true;

    internal string RuntimePackUsabilityStatus
    {
        get
        {
            if (RuntimePack == null)
                return "not inspected";
            if (!RuntimePack.Usable)
                return RuntimePack.Status;
            return "usable";
        }
    }

    internal bool SourceMatchesActiveAndroidAssembly =>
        !string.IsNullOrWhiteSpace(SourceAssemblySha256)
        && !string.IsNullOrWhiteSpace(ActiveAndroidAssemblySha256)
        && string.Equals(SourceAssemblySha256, ActiveAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);

    internal string PreparedAndroidAssemblySha256 =>
        RuntimePackUsable
            ? RuntimePack.ActualAndroidAssemblySha256
            : "<missing>";

    internal bool ActiveAndroidAssemblyMatchesPreparedRuntime =>
        ActiveAndroidAssemblyExists
        && !string.IsNullOrWhiteSpace(PreparedAndroidAssemblySha256)
        && !PreparedAndroidAssemblySha256.StartsWith("<", StringComparison.Ordinal)
        && string.Equals(
            ActiveAndroidAssemblySha256,
            PreparedAndroidAssemblySha256,
            StringComparison.OrdinalIgnoreCase
        );

    internal bool RequiresProcessRestartForPreparedRuntime =>
        RuntimePackUsable
        && !ActiveAndroidAssemblyMatchesPreparedRuntime;

    internal bool BranchRuntimeAvailable =>
        GameIdentity != null && RuntimePackUsable;

    internal bool RequiresRuntimePackOrPreparedCache => true;

    internal bool RuntimeCompatible => BranchRuntimeAvailable;

    internal bool PatchCompatible => PatchCompatibility?.Passed == true;

    internal bool Playable => GameIdentity != null && RuntimeCompatible && PatchCompatible;

    internal string RuntimePairingStatus
    {
        get
        {
            if (!ActiveAndroidAssemblyExists)
            {
                if (RuntimePackUsable)
                    return "runtime pack available; Android cache will be prepared at launch";
                return "missing Android runtime assembly";
            }
            if (RuntimePackUsable)
                return ActiveAndroidAssemblyMatchesPreparedRuntime
                    ? "prepared runtime pack is active"
                    : "runtime pack available; Android cache will be prepared at launch";
            if (!SourceAssemblyExists)
                return RuntimePackManifestExists
                    ? $"runtime pack not usable: {RuntimePackUsabilityStatus}"
                    : "missing selected-branch source assembly";
            if (SourceAssemblyExists)
                return RuntimePackManifestExists
                    ? $"runtime pack not usable: {RuntimePackUsabilityStatus}"
                    : "selected branch source assembly is present, but a usable runtime pack is required";
            return "runtime pack required";
        }
    }

    internal string ReadinessProblem()
    {
        if (!RuntimeCompatible)
            return RuntimeReadinessProblem();

        var patchProblem = PatchCompatibility?.Problem;
        if (!string.IsNullOrWhiteSpace(patchProblem))
            return patchProblem;

        return null;
    }

    private string RuntimeReadinessProblem()
    {
        if (GameIdentity == null)
            return PreparationFailedMessage();

        if (!HasUsableHash(PckSha256))
            return "Download the selected branch to continue.";

        if (!SourceAssemblyExists)
            return PreparationFailedMessage();

        if (!RuntimePackUsable)
            return PreparationFailedMessage();

        if (!ActiveAndroidAssemblyExists && !RuntimePackUsable)
            return PreparationFailedMessage();

        return PreparationFailedMessage();
    }

    private static string PreparationFailedMessage()
        => "Game preparation failed for the selected branch. Repair selected branch, then try again. If it happens again, create a new support report.";
}
