using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeCacheEvidence
{
    internal static bool MatchesSelectedBranch(string dataDir, string selectedBranch)
    {
        var markerBranch = SelectedBranch(dataDir);
        return HasValue(markerBranch)
            && string.Equals(
                SteamGameBranch.Normalize(markerBranch),
                SteamGameBranch.Normalize(selectedBranch),
                StringComparison.OrdinalIgnoreCase
            );
    }

    internal static bool PckMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerHash = SelectedPckSha256(dataDir);
        if (!HasValue(markerHash))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        if (HasValue(slot.PckSha256)
            && string.Equals(markerHash, slot.PckSha256, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return slot.RuntimePackUsable
            && slot.RuntimePack.SourcePckMatchesSelectedPck(markerHash, slot.PckPath);
    }

    internal static bool SourceAssemblyMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerHash = SelectedSourceAssemblySha256(dataDir);
        if (!HasValue(markerHash))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        return HasValue(slot.SourceAssemblySha256)
            && string.Equals(markerHash, slot.SourceAssemblySha256, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool PublishCacheMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerHash = PublishCacheActiveAssemblySha256(dataDir);
        if (!HasValue(markerHash))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        if (slot.RuntimePackUsable)
            return HasValue(slot.RuntimePack.ActualAndroidAssemblySha256)
                && string.Equals(markerHash, slot.RuntimePack.ActualAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);

        if (slot.RequiresRuntimePackOrPreparedCache)
            return false;

        if (slot.SourceAssemblyExists)
            return string.Equals(markerHash, slot.SourceAssemblySha256, StringComparison.OrdinalIgnoreCase);

        return slot.UsesLegacyPackagedPublicRuntime
            && HasValue(slot.ActiveAndroidAssemblySha256)
            && string.Equals(markerHash, slot.ActiveAndroidAssemblySha256, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool CachePreparedForSelectedRuntime(string dataDir, string selectedBranch)
        => MarkerPresent(dataDir)
            && MatchesSelectedBranch(dataDir, selectedBranch)
            && PckMatchesSelectedRuntime(dataDir, selectedBranch)
            && SourceAssemblyMatchesSelectedRuntime(dataDir, selectedBranch)
            && PublishCacheMatchesSelectedRuntime(dataDir, selectedBranch);
}
