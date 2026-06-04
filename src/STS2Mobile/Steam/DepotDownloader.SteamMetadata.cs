using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly ProductInfoAppCache _productInfoAppCache = new();

    private async Task<List<DepotManifestReference>> PrepareAndGetMainAppDepotsAsync(
        bool requireAny
    )
    {
        _stateStore.Prepare();

        var depots = await GetMainAppDepotsAsync();
        if (requireAny && depots.Count == 0)
            throw new Exception("No downloadable depots found");

        return depots;
    }

    private async Task<List<DepotManifestReference>> GetMainAppDepotsAsync()
    {
        var depotSection = await ProductInfoApp.GetMainDepotsSectionAsync(this);
        return await ParseDepotsAsync(depotSection);
    }
}
