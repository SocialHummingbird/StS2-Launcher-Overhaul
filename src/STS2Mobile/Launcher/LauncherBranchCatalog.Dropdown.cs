using System;
using System.Collections.Generic;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    internal static IReadOnlyList<BranchOption> DropdownOptions(
        string selectedBranch,
        IReadOnlyList<BranchOption> discoveredBranches
    )
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        var options = new List<BranchOption>
        {
            new(SteamGameBranch.Public, source: "fallback")
        };

        foreach (var branch in discoveredBranches ?? Array.Empty<BranchOption>())
            AddOrReplace(options, branch);

        AddIfMissing(options, new BranchOption(selectedBranch, source: "saved selection"));

        return options;
    }

    internal static string DropdownOptionLabels(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
        => string.Join(" | ", DropdownOptions(selectedBranch, discoveredBranches).Select(option => option.Label));
}
