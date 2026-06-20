using System;
using System.IO;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal sealed partial class GameRuntimeSlot
{
    private static string CachedSelectedPckSha256(string dataDir, string branch, string pckPath)
    {
        try
        {
            var cachedBranch = LauncherRuntimeCacheEvidence.SelectedBranch(dataDir);
            if (HasMarkerValue(cachedBranch)
                && !string.Equals(
                    SteamGameBranch.Normalize(cachedBranch),
                    SteamGameBranch.Normalize(branch),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return null;
            }

            var cachedPath = LauncherRuntimeCacheEvidence.SelectedPckPath(dataDir);
            if (!PathsEquivalent(cachedPath, pckPath, dataDir))
                return null;

            var cachedIdentity = LauncherRuntimeCacheEvidence.SelectedPckIdentity(dataDir);
            if (!FileIdentityMatches(cachedIdentity, pckPath))
                return null;

            var cachedRuntimeSource = LauncherRuntimeCacheEvidence.RuntimeSource(dataDir);
            if (HasMarkerValue(cachedRuntimeSource)
                && string.Equals(cachedRuntimeSource, "no-usable-runtime", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var cachedHash = LauncherRuntimeCacheEvidence.SelectedPckSha256(dataDir);
            return HasUsableHash(cachedHash)
                ? cachedHash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string CachedSelectedSourceAssemblySha256(string dataDir, string branch, string sourceAssemblyPath)
    {
        try
        {
            var cachedBranch = LauncherRuntimeCacheEvidence.SelectedBranch(dataDir);
            if (HasMarkerValue(cachedBranch)
                && !string.Equals(
                    SteamGameBranch.Normalize(cachedBranch),
                    SteamGameBranch.Normalize(branch),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return null;
            }

            var cachedPath = LauncherRuntimeCacheEvidence.SelectedSourceAssembly(dataDir);
            if (!PathsEquivalent(cachedPath, sourceAssemblyPath, dataDir))
                return null;

            var cachedHash = LauncherRuntimeCacheEvidence.SelectedSourceAssemblySha256(dataDir);
            return HasUsableHash(cachedHash)
                ? cachedHash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static string CachedActiveAndroidAssemblySha256(string dataDir, string branch, string activeAssemblyPath)
    {
        try
        {
            var cachedBranch = LauncherRuntimeCacheEvidence.SelectedBranch(dataDir);
            if (HasMarkerValue(cachedBranch)
                && !string.Equals(
                    SteamGameBranch.Normalize(cachedBranch),
                    SteamGameBranch.Normalize(branch),
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return null;
            }

            var publishDirectory = LauncherRuntimeCacheEvidence.PublishCacheDirectory(dataDir);
            if (!HasMarkerValue(publishDirectory) || string.IsNullOrWhiteSpace(activeAssemblyPath))
                return null;

            var cachedPath = Path.Combine(publishDirectory, GameAssemblyFileName);
            if (!PathsEquivalent(cachedPath, activeAssemblyPath, dataDir))
                return null;

            var cachedHash = LauncherRuntimeCacheEvidence.PublishCacheActiveAssemblySha256(dataDir);
            return HasUsableHash(cachedHash)
                ? cachedHash.ToLowerInvariant()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
