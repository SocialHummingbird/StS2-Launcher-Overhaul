using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ProductInfoApp
    {
        private ProductInfoApp(uint appId)
        {
            AppId = appId;
        }

        private uint AppId { get; }
        private bool IsMainApp => AppId == SteamCloudApp.AppId;
        private string Name => IsMainApp
            ? $"{SteamCloudApp.Name} ({SteamCloudApp.AppId})"
            : $"referenced app {AppId}";
        private string OwnershipHint => IsMainApp
            ? "; ownership/session may be invalid"
            : "";

        internal static ProductInfoApp Create(uint appId)
            => new(appId);

        internal string AccessTokenDenied()
            => $"Steam denied app access token for {Name}{OwnershipHint}";

        internal string PublicAccessTokenFallback()
            => $"Steam returned no app access token for {Name}; "
                + "continuing with public token 0";

        internal string AppInfoUnavailable()
            => $"Failed to get app info from Steam for {Name}{OwnershipHint}";

        internal string MissingDepotsSection()
            => $"Steam app info for {Name} has no depots section";
    }

    private async Task<ulong> GetProductInfoAccessTokenAsync(uint appId)
    {
        var app = ProductInfoApp.Create(appId);
        var token = await _connection.GetAppAccessTokenOrPublicAsync(
            appId,
            app.AccessTokenDenied()
        );
        if (token == 0)
            Log(app.PublicAccessTokenFallback());

        return token;
    }
}
