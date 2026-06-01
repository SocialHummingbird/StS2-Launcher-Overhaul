using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task<ulong> GetProductInfoAccessTokenAsync(uint appId)
    {
        var token = await _connection.GetAppAccessTokenOrPublicAsync(
            appId,
            ProductInfoAccessTokenDenied(appId)
        );
        if (token == 0)
            Log(ProductInfoPublicAccessTokenFallback(appId));

        return token;
    }

    private static string ProductInfoAccessTokenDenied(uint appId)
        => $"Steam denied app access token for {ProductInfoAppName(appId)}"
            + ProductInfoOwnershipHint(appId);

    private static string ProductInfoPublicAccessTokenFallback(uint appId)
        => $"Steam returned no app access token for {ProductInfoAppName(appId)}; "
            + "continuing with public token 0";

    private static string ProductInfoAppName(uint appId)
        => appId == SteamCloudApp.AppId
            ? $"{SteamCloudApp.Name} ({SteamCloudApp.AppId})"
            : $"referenced app {appId}";

    private static string ProductInfoOwnershipHint(uint appId)
        => appId == SteamCloudApp.AppId
            ? "; ownership/session may be invalid"
            : "";
}
