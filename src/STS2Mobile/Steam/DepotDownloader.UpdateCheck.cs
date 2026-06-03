using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    // Returns true if any depot has a newer manifest than what's cached locally.
    internal Task<bool> CheckForUpdatesAsync(CancellationToken ct = default)
        => RunWithSuspendedIdleTimeoutAsync(() => CheckForUpdatesCoreAsync(ct));

    private async Task<bool> CheckForUpdatesCoreAsync(CancellationToken ct)
    {
        var depots = await PrepareAndGetMainAppDepotsAsync(requireAny: false);

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
