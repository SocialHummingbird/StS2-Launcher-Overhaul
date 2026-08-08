using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static string CanonicalizeRuntimePackSourcePckSha256(
        string pckSha256,
        RuntimePackManifest runtimePack
    )
    {
        if (runtimePack?.Usable == true
            && HasUsableHash(runtimePack.SourcePckSha256)
            && !string.Equals(runtimePack.SourcePckSha256, pckSha256, StringComparison.OrdinalIgnoreCase))
        {
            PatchHelper.Log($"[Launcher] Runtime slot inspect phase: canonicalizing Android-patched PCK hash {pckSha256} to runtime source PCK hash {runtimePack.SourcePckSha256}");
            return runtimePack.SourcePckSha256.ToLowerInvariant();
        }

        return pckSha256;
    }
}
