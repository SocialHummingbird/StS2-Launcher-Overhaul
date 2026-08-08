using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly ProductInfoAppCache _productInfoAppCache = new();
    private BranchAvailabilityReport _lastBranchAvailability;

    private async Task<List<DepotManifestReference>> PrepareAndGetMainAppDepotsAsync(
        bool requireAny
    )
    {
        _stateStore.Prepare();

        var depots = await GetMainAppDepotsAsync();
        if (depots.Count == 0)
        {
            if (requireAny || !IsPublicBranch)
                throw new Exception(NoAccessibleDepotsMessage());
        }

        return depots;
    }

    private async Task<List<DepotManifestReference>> GetMainAppDepotsAsync()
    {
        var depotSection = await ProductInfoApp.GetMainDepotsSectionAsync(this);
        _lastBranchAvailability = BranchAvailabilityReport.FromDepots(depotSection, _branch);
        Log(_lastBranchAvailability.LogSummary());
        _lastBranchAvailability.WriteMarker(_dataDir);
        return await ParseDepotsAsync(depotSection);
    }

    private bool IsPublicBranch
        => string.Equals(_branch, SteamGameBranch.Public, StringComparison.OrdinalIgnoreCase);

    private string NoAccessibleDepotsMessage()
        => IsPublicBranch
            ? "No downloadable depots found"
            : $"No downloadable depots found for Steam branch '{_branch}'. "
                + "The branch may not exist, may be unavailable to this Steam account, "
                + "or may require a Steam beta password. Beta password entry is not implemented yet. "
                + (_lastBranchAvailability?.FailureSummary() ?? "Visible branch information was unavailable.");
}
