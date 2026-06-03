using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly ConcurrentDictionary<
        uint,
        SteamApps.PICSProductInfoCallback.PICSProductInfo
    > _appInfoCache = new();

    private readonly struct DepotManifestReference
    {
        internal DepotManifestReference(uint depotId, ulong manifestId)
        {
            DepotId = depotId;
            ManifestId = manifestId;
        }

        internal uint DepotId { get; }
        internal ulong ManifestId { get; }
    }

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
