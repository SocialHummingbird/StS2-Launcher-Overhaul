using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    internal bool RuntimePackUsable => RuntimePack?.Usable == true && RuntimePackSlotIdMatches;

    internal string RuntimePackUsabilityStatus
    {
        get
        {
            if (RuntimePack == null)
                return "not inspected";
            if (!RuntimePack.Usable)
                return RuntimePack.Status;
            if (string.IsNullOrWhiteSpace(RuntimePack.SourceRuntimeSlotId))
                return "missing source runtime slot ID";
            if (!RuntimePackSlotIdMatches)
                return "runtime slot ID mismatch";
            return "usable";
        }
    }

    internal bool SourceMatchesActiveAndroidAssembly =>
        !string.IsNullOrWhiteSpace(SourceAssemblySha256)
        && !string.IsNullOrWhiteSpace(ActiveAndroidAssemblySha256)
        && string.Equals(SourceAssemblySha256, ActiveAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);

    internal bool BranchMatchedAndroidRuntimePrepared =>
        SourceAssemblyExists
        && ActiveAndroidAssemblyExists
        && SourceMatchesActiveAndroidAssembly;

    internal bool BranchRuntimeAvailable =>
        RuntimePackUsable
        || (!RequiresRuntimePackOrPreparedCache && BranchMatchedAndroidRuntimePrepared);

    internal bool UsesLegacyPackagedPublicRuntime =>
        string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase)
        && (ActiveAndroidAssemblyExists || SourceAssemblyExists);

    internal bool RequiresRuntimePackOrPreparedCache =>
        !string.Equals(Branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase);

    internal bool RuntimeCompatible =>
        BranchRuntimeAvailable
        || UsesLegacyPackagedPublicRuntime;

    internal bool PatchCompatible => PatchCompatibility?.Passed == true;

    internal bool Playable => RuntimeCompatible && PatchCompatible;

    internal string RuntimePairingStatus
    {
        get
        {
            if (!ActiveAndroidAssemblyExists)
            {
                if (UsesLegacyPackagedPublicRuntime)
                    return "legacy public runtime available; Android cache will be prepared at launch";
                if (RuntimePackUsable)
                    return "runtime pack available; Android cache will be prepared at launch";
                return "missing Android runtime assembly";
            }
            if (SourceMatchesActiveAndroidAssembly && !RequiresRuntimePackOrPreparedCache)
                return "branch-matched runtime";
            if (RuntimePackUsable)
                return "runtime pack available; Android cache will be prepared at launch";
            if (UsesLegacyPackagedPublicRuntime)
                return "legacy packaged public runtime";
            if (!SourceAssemblyExists)
                return RuntimePackManifestExists
                    ? $"runtime pack not usable: {RuntimePackUsabilityStatus}"
                    : "missing selected-branch source assembly";
            if (SourceAssemblyExists)
                return RuntimePackManifestExists
                    ? $"runtime pack not usable: {RuntimePackUsabilityStatus}"
                    : RequiresRuntimePackOrPreparedCache
                        ? "selected branch source assembly is present, but non-public versions require a usable runtime pack"
                        : "selected branch source assembly is present, but no usable Android runtime pack or prepared branch-matched cache exists";
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
        if (!HasUsableHash(PckSha256))
            return "Selected game version is not downloaded or the downloaded PCK is invalid. Download selected version to continue.";

        if (!SourceAssemblyExists)
        {
            if (RuntimePackManifestExists && !RuntimePackUsable)
                return $"Selected game version is missing its source game-code assembly and its runtime pack is not usable ({RuntimePackUsabilityStatus}). Redownload selected version or install a matching runtime pack.";

            return "Selected game version is missing its source game-code assembly. Redownload selected version.";
        }

        if (RequiresRuntimePackOrPreparedCache && !RuntimePackUsable)
            return RuntimePackManifestExists
                ? $"Selected game version requires a usable runtime pack, but its runtime pack is not usable ({RuntimePackUsabilityStatus}). Redownload selected version."
                : "Selected game version requires a usable runtime pack. Redownload selected version to regenerate runtime-pack evidence.";

        if (!ActiveAndroidAssemblyExists && !RuntimePackUsable)
            return "Android game-code runtime cache is missing and no usable runtime pack exists. Redownload selected version to rebuild runtime evidence.";

        return "Selected game version is downloaded, but its Android game-code runtime does not match the selected Steam branch. Install a matching runtime pack or select a compatible version.";
    }
}
