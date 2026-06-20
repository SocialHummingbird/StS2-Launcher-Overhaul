using System;
using System.Collections.Generic;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    private static void AddIfMissing(List<BranchOption> options, BranchOption option)
    {
        if (!options.Exists(existing => string.Equals(existing.Branch, option.Branch, StringComparison.OrdinalIgnoreCase)))
            options.Add(option);
    }

    private static void AddOrReplace(List<BranchOption> options, BranchOption option)
    {
        var existingIndex = options.FindIndex(existing => string.Equals(existing.Branch, option.Branch, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
            options[existingIndex] = option;
        else
            options.Add(option);
    }
}
