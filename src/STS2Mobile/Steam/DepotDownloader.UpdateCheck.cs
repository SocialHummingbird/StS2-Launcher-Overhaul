using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    // Returns true if any depot has a newer manifest than what's cached locally.
    internal async Task<bool> CheckForUpdatesAsync(CancellationToken ct = default)
        => await RunWithSuspendedIdleTimeoutAsync(() => CheckForUpdatesCoreAsync(ct));

    private async Task<bool> CheckForUpdatesCoreAsync(CancellationToken ct)
    {
        _stateStore.Prepare();

        var depots = await GetMainAppDepotsAsync();

        foreach (var depot in depots)
        {
            ct.ThrowIfCancellationRequested();
            if (_stateStore.LoadManifestId(depot.DepotId) != depot.ManifestId)
            {
                Log($"Update available: depot {depot.DepotId} manifest changed");
                return true;
            }
        }

        Log("Game is up to date");
        return false;
    }
}
