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
            return string.IsNullOrWhiteSpace(GameIdentityProblem)
                ? "Selected game identity could not be calculated from the current installed files. Redownload selected version."
                : GameIdentityProblem;

        if (!HasUsableHash(PckSha256))
            return "Selected game version is not downloaded or the downloaded PCK is invalid. Download selected version to continue.";

        if (!SourceAssemblyExists)
        {
            if (RuntimePackManifestExists && !RuntimePackUsable)
                return $"Selected game version is missing its source game-code assembly and its runtime pack is not usable ({RuntimePackUsabilityStatus}). Redownload selected version or install a matching runtime pack.";

            return "Selected game version is missing its source game-code assembly. Redownload selected version.";
        }

        if (!RuntimePackUsable)
            return RuntimePackManifestExists
                ? $"Selected game version requires a usable runtime pack, but its runtime pack is not usable ({RuntimePackUsabilityStatus}). Redownload selected version."
                : "Selected game version requires a usable runtime pack. Redownload selected version to regenerate runtime-pack evidence.";

        if (!ActiveAndroidAssemblyExists && !RuntimePackUsable)
            return "Android game-code runtime cache is missing and no usable runtime pack exists. Redownload selected version to rebuild runtime evidence.";

        return "Selected game version is downloaded, but its Android game-code runtime does not match the selected Steam branch. Install a matching runtime pack or select a compatible version.";
    }
}
