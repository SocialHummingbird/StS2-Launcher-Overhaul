using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherSaveOriginEvidence
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

    internal static bool CurrentLocalSavesMatchSelectedBranch(string dataDir, string selectedBranch)
        => MarkerPresent(dataDir)
            && OriginUtcParseable(dataDir)
            && MatchesSelectedBranch(dataDir, selectedBranch)
            && LauncherLocalSaveEvidence.HasImportantSaveEvidence(dataDir);

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

    internal static bool RuntimeSlotIdMatchesSelectedRuntime(string dataDir, string selectedBranch)
    {
        var markerSlotId = SelectedRuntimeSlotId(dataDir);
        if (!HasValue(markerSlotId))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        return HasValue(slot.RuntimeSlotId)
            && string.Equals(markerSlotId, slot.RuntimeSlotId, StringComparison.OrdinalIgnoreCase);
    }

    internal static bool SelectedRuntimeCurrentlyPlayable(string dataDir, string selectedBranch)
        => GameRuntimeSlot.Inspect(dataDir, selectedBranch).Playable;

    internal static bool CurrentLocalSavesMatchSelectedRuntime(string dataDir, string selectedBranch)
    {
        if (!CurrentLocalSavesMatchSelectedBranch(dataDir, selectedBranch))
            return false;

        var markerSlotId = SelectedRuntimeSlotId(dataDir);
        var markerSourceAssemblySha256 = SelectedSourceAssemblySha256(dataDir);
        if (!HasValue(markerSlotId) || !HasValue(markerSourceAssemblySha256))
            return false;

        var slot = GameRuntimeSlot.Inspect(dataDir, selectedBranch);
        return slot.Playable
            && HasValue(slot.RuntimeSlotId)
            && HasValue(slot.SourceAssemblySha256)
            && string.Equals(markerSlotId, slot.RuntimeSlotId, StringComparison.OrdinalIgnoreCase)
            && PckMatchesSelectedRuntime(dataDir, selectedBranch)
            && string.Equals(markerSourceAssemblySha256, slot.SourceAssemblySha256, StringComparison.OrdinalIgnoreCase);
    }
}
