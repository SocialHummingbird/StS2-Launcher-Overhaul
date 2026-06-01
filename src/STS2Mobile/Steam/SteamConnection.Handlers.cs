using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SteamKit2;
using SteamKit2.CDN;

namespace STS2Mobile.Steam;

internal sealed partial class SteamConnection
{
    internal Client CreateCdnClient()
        => new(_client);

    internal async Task<IReadOnlyCollection<Server>> LoadCdnServersAsync(CancellationToken ct)
        => await ContentServerDirectoryService.LoadAsync(_client.Configuration, ct);

    internal async Task<byte[]> GetDepotDecryptionKeyAsync(uint depotId)
    {
        EnsureConnected();

        var result = await _steamApps.GetDepotDecryptionKey(depotId, SteamCloudApp.AppId);
        if (result.Result != EResult.OK)
            throw new InvalidOperationException(
                $"Failed to get depot key for {depotId}: {result.Result}"
            );

        return result.DepotKey;
    }

    internal async Task<ulong> GetManifestRequestCodeAsync(
        uint depotId,
        ulong manifestId,
        string branch
    )
    {
        EnsureConnected();

        return await _steamContent.GetManifestRequestCode(
            depotId,
            SteamCloudApp.AppId,
            manifestId,
            branch
        );
    }

    internal async Task<string?> GetCdnAuthTokenAsync(uint depotId, string host)
    {
        EnsureConnected();

        var result = await _steamContent.GetCDNAuthToken(
            SteamCloudApp.AppId,
            depotId,
            host
        );
        return result.Result == EResult.OK ? result.Token : null;
    }

    internal async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> GetAppInfoAsync(
        uint appId,
        ulong accessToken
    )
    {
        EnsureConnected();

        var infoResult = await _steamApps.PICSGetProductInfo(
            new[] { new SteamApps.PICSRequest(appId, accessToken) },
            Enumerable.Empty<SteamApps.PICSRequest>()
        );

        foreach (var cb in infoResult.Results)
            if (cb.Apps.TryGetValue(appId, out var info))
                return info;

        return null;
    }
}
