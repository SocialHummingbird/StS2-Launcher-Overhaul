using System;
using System.Collections.Generic;
using Godot;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher.Sections;

internal static class LauncherBranchDropdown
{
    internal readonly struct SelectionUpdate
    {
        internal SelectionUpdate(string branch, bool changed)
        {
            Branch = branch;
            Changed = changed;
        }

        internal string Branch { get; }
        internal bool Changed { get; }
    }

    internal static SelectionUpdate NormalizeSelection(string currentBranch, string requestedBranch)
    {
        var normalizedCurrentBranch = SteamGameBranch.Normalize(currentBranch);
        var normalizedRequestedBranch = SteamGameBranch.Normalize(requestedBranch);
        return new SelectionUpdate(
            normalizedRequestedBranch,
            !string.Equals(normalizedCurrentBranch, normalizedRequestedBranch, StringComparison.OrdinalIgnoreCase)
        );
    }

    internal static IReadOnlyList<LauncherBranchCatalog.BranchOption> NormalizeAvailableBranches(
        IReadOnlyList<LauncherBranchCatalog.BranchOption> branches
    )
        => branches ?? Array.Empty<LauncherBranchCatalog.BranchOption>();

    internal static void Populate(
        OptionButton dropdown,
        List<LauncherBranchCatalog.BranchOption> branchOptions,
        string selectedBranch,
        IReadOnlyList<LauncherBranchCatalog.BranchOption> availableBranches
    )
    {
        selectedBranch = SteamGameBranch.Normalize(selectedBranch);
        branchOptions.Clear();
        dropdown.Clear();

        var selectedIndex = 0;
        foreach (var option in LauncherBranchCatalog.DropdownOptions(selectedBranch, availableBranches))
        {
            var index = branchOptions.Count;
            branchOptions.Add(option);
            dropdown.AddItem(option.Label);

            if (string.Equals(option.Branch, selectedBranch, StringComparison.OrdinalIgnoreCase))
                selectedIndex = index;
        }

        dropdown.Select(selectedIndex);
    }

    internal static bool TryGetBranch(
        List<LauncherBranchCatalog.BranchOption> branchOptions,
        long index,
        out string branch
    )
    {
        if (index < 0 || index >= branchOptions.Count)
        {
            branch = SteamGameBranch.Public;
            return false;
        }

        branch = branchOptions[(int)index].Branch;
        return true;
    }
}
