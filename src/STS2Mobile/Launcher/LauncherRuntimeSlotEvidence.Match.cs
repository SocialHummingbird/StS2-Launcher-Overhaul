using System;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherRuntimeSlotEvidence
{
    internal static bool BranchMatchesSelectedRuntime(string dataDir, string branch)
    {
        var markerBranch = Branch(dataDir);
        if (IsMissing(markerBranch))
            return false;

        return string.Equals(
            SteamGameBranch.Normalize(markerBranch),
            SteamGameBranch.Normalize(branch),
            StringComparison.OrdinalIgnoreCase
        );
    }

    internal static bool RuntimeSlotIdMatchesSelectedRuntime(string dataDir, string branch)
        => MarkerValueMatchesSelectedRuntime(
            dataDir,
            branch,
            RuntimeSlotIdProperty,
            slot => slot.RuntimeSlotId
        );

    internal static bool PckMatchesSelectedRuntime(string dataDir, string branch)
        => MarkerValueMatchesSelectedRuntime(
            dataDir,
            branch,
            PckSha256Property,
            slot => slot.PckSha256
        );

    internal static bool SourceAssemblyMatchesSelectedRuntime(string dataDir, string branch)
        => MarkerValueMatchesSelectedRuntime(
            dataDir,
            branch,
            SourceAssemblySha256Property,
            slot => slot.SourceAssemblySha256
        );

    private static bool MarkerValueMatchesSelectedRuntime(
        string dataDir,
        string branch,
        string property,
        Func<GameRuntimeSlot, string> selectedValue
    )
    {
        var markerValue = ReadString(dataDir, property);
        if (IsMissing(markerValue))
            return false;

        var selectedRuntime = GameRuntimeSlot.Inspect(dataDir, branch);
        return string.Equals(
            markerValue,
            selectedValue(selectedRuntime),
            StringComparison.OrdinalIgnoreCase
        );
    }
}
