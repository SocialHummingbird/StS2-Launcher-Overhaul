using System;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static string PckSha256OrMissing(string dataDir, string branch, string gameDirectory, string pckPath, string runtimePackManifestPath)
    {
        var cached = CachedSelectedPckSha256(dataDir, branch, pckPath);
        if (HasUsableHash(cached))
            return cached;

        if (!string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
        {
            var validated = ValidatedGameDirectoryPckSha256(gameDirectory, branch);
            if (HasUsableHash(validated))
                return validated;

            var runtimePack = RuntimePackSourcePckSha256(runtimePackManifestPath, branch);
            if (HasUsableHash(runtimePack))
                return runtimePack;

            return Sha256OrMissing(pckPath);
        }

        return Sha256OrMissing(pckPath);
    }

    private static string SourceAssemblySha256OrMissing(string dataDir, string branch, string sourceAssemblyPath)
    {
        var cached = CachedSelectedSourceAssemblySha256(dataDir, branch, sourceAssemblyPath);
        if (HasUsableHash(cached))
            return cached;

        if (!string.Equals(branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase))
        {
            var gameDirectory = Directory.GetParent(Path.GetDirectoryName(sourceAssemblyPath) ?? string.Empty)?.FullName;
            var validated = ValidatedGameDirectorySourceAssemblySha256(gameDirectory, branch);
            if (HasUsableHash(validated))
                return validated;

            var runtimePackManifestPath = BuildRuntimePackManifestPath(dataDir, branch);
            var runtimePack = RuntimePackSourceAssemblySha256(runtimePackManifestPath, branch);
            if (HasUsableHash(runtimePack))
                return runtimePack;
        }

        return Sha256OrMissing(sourceAssemblyPath);
    }

    private static string ActiveAndroidAssemblySha256OrMissing(string dataDir, string branch, string activeAssemblyPath)
    {
        var cached = CachedActiveAndroidAssemblySha256(dataDir, branch, activeAssemblyPath);
        return HasUsableHash(cached)
            ? cached
            : Sha256OrMissing(activeAssemblyPath);
    }
}
