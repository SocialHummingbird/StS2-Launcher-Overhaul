using System;
using System.Text;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static string BuildRuntimeSlotIdentity(
        string branch,
        RuntimeSlotMetadata metadata,
        RuntimePackManifest runtimePack,
        bool runtimePackSlotIdMatches,
        PatchCompatibilityEvidence patchCompatibility,
        string pckSha256,
        string sourceAssemblySha256
    )
    {
        var runtimePackUsable = runtimePack?.Usable == true && runtimePackSlotIdMatches;
        var requiresRuntimePack = !string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase);
        var runtimeSource = runtimePackUsable
            ? "runtime-pack"
            : requiresRuntimePack
                ? "no-usable-runtime"
                : "selected-game";
        var androidAssemblySha256 = runtimePackUsable
            ? runtimePack.ActualAndroidAssemblySha256
            : sourceAssemblySha256;
        return string.Join(
            "\n",
            $"branch={SteamGameBranch.Normalize(branch)}",
            $"stateDirectory={SteamGameBranch.StateDirectoryName(branch)}",
            $"releaseVersion={metadata?.ReleaseVersion ?? string.Empty}",
            $"releaseCommit={metadata?.ReleaseCommit ?? string.Empty}",
            $"releaseBuildId={metadata?.ReleaseBuildId ?? string.Empty}",
            $"depotManifestFingerprint={metadata?.DepotManifestFingerprint ?? string.Empty}",
            $"pckSha256={pckSha256}",
            $"sourceAssemblySha256={sourceAssemblySha256}",
            $"runtimeSource={runtimeSource}",
            $"runtimePackId={runtimePack?.PackId ?? string.Empty}",
            $"androidAssemblySha256={androidAssemblySha256}",
            $"patchSetVersion={patchCompatibility?.PatchSetVersion ?? runtimePack?.PatchSetVersion ?? string.Empty}",
            $"patchValidationStatus={patchCompatibility?.Status ?? runtimePack?.PatchValidationStatus ?? string.Empty}"
        );
    }

    private static bool RuntimePackSlotIdMatchesFor(
        RuntimeSlotMetadata metadata,
        RuntimePackManifest runtimePack,
        string branch,
        string pckSha256,
        string sourceAssemblySha256
    )
    {
        if (runtimePack == null || !runtimePack.Exists || !runtimePack.Readable)
            return false;
        if (string.IsNullOrWhiteSpace(runtimePack.SourceRuntimeSlotId))
            return false;
        if (string.IsNullOrWhiteSpace(runtimePack.PackId))
            return false;

        var expectedIdentity = string.Join(
            "\n",
            $"branch={SteamGameBranch.Normalize(branch)}",
            $"stateDirectory={SteamGameBranch.StateDirectoryName(branch)}",
            $"releaseVersion={metadata?.ReleaseVersion ?? string.Empty}",
            $"releaseCommit={metadata?.ReleaseCommit ?? string.Empty}",
            $"releaseBuildId={metadata?.ReleaseBuildId ?? string.Empty}",
            $"depotManifestFingerprint={metadata?.DepotManifestFingerprint ?? string.Empty}",
            $"pckSha256={pckSha256}",
            $"sourceAssemblySha256={sourceAssemblySha256}",
            "runtimeSource=runtime-pack",
            $"runtimePackId={runtimePack.PackId}",
            $"androidAssemblySha256={runtimePack.ActualAndroidAssemblySha256}",
            $"patchSetVersion={runtimePack.PatchSetVersion}",
            $"patchValidationStatus={runtimePack.PatchValidationStatus}"
        );
        var expectedId = BuildRuntimeSlotId(branch, expectedIdentity);
        return string.Equals(runtimePack.SourceRuntimeSlotId, expectedId, StringComparison.OrdinalIgnoreCase);
    }

    internal static string BuildRuntimePackSlotIdentity(GameRuntimeSlot slot, string patchSetVersion, string runtimePackId)
    {
        return string.Join(
            "\n",
            $"branch={SteamGameBranch.Normalize(slot.Branch)}",
            $"stateDirectory={SteamGameBranch.StateDirectoryName(slot.Branch)}",
            $"releaseVersion={slot.Metadata?.ReleaseVersion ?? string.Empty}",
            $"releaseCommit={slot.Metadata?.ReleaseCommit ?? string.Empty}",
            $"releaseBuildId={slot.Metadata?.ReleaseBuildId ?? string.Empty}",
            $"depotManifestFingerprint={slot.Metadata?.DepotManifestFingerprint ?? string.Empty}",
            $"pckSha256={slot.PckSha256}",
            $"sourceAssemblySha256={slot.SourceAssemblySha256}",
            "runtimeSource=runtime-pack",
            $"runtimePackId={runtimePackId}",
            $"androidAssemblySha256={slot.SourceAssemblySha256}",
            $"patchSetVersion={patchSetVersion}",
            "patchValidationStatus=passed"
        );
    }

    internal static string BuildRuntimePackSlotId(GameRuntimeSlot slot, string patchSetVersion, string runtimePackId)
        => BuildRuntimeSlotId(
            slot.Branch,
            BuildRuntimePackSlotIdentity(slot, patchSetVersion, runtimePackId)
        );

    private static string BuildRuntimeSlotId(string branch, string runtimeSlotIdentity)
        => $"{SteamGameBranch.StateDirectoryName(branch)}-{StableHash16(runtimeSlotIdentity)}";

    private static string StableHash16(string value)
    {
        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offset;
            foreach (var b in Encoding.UTF8.GetBytes(value ?? string.Empty))
            {
                hash ^= b;
                hash *= prime;
            }

            return hash.ToString("x16");
        }
    }
}