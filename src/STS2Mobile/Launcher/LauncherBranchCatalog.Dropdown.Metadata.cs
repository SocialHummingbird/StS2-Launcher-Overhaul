using System;
using System.Collections.Generic;
using System.Linq;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherBranchCatalog
{
    internal static string DropdownOptionMetadata(string selectedBranch, IReadOnlyList<BranchOption> discoveredBranches)
    {
        var options = DropdownOptions(selectedBranch, discoveredBranches);
        return string.Join(
            " | ",
            options.Select(option =>
                $"{option.Branch}:source={option.Source};"
                + $"{SteamBranchAvailabilityMarkerFields.MetadataVisibleKey}={option.MetadataVisible.ToString().ToLowerInvariant()};"
                + $"{SteamBranchAvailabilityMarkerFields.WindowsManifestDepotsKey}={option.WindowsManifestDepotCount};"
                + $"{SteamBranchAvailabilityMarkerFields.PasswordRequiredKey}={ValueOrUnknown(option.PasswordRequired)};"
                + $"{SteamBranchAvailabilityMarkerFields.BuildIdKey}={ValueOrNone(option.BuildId)}"
            )
        );
    }

    private static string ValueOrUnknown(string value)
        => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();

    private static string ValueOrNone(string value)
        => string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
}
