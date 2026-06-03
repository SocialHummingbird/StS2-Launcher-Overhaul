using System.Threading.Tasks;
using SteamKit2;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly struct ProductInfoApp
    {
        private ProductInfoApp(uint appId)
        {
            AppId = appId;
        }

        internal uint AppId { get; }
        private bool IsMainApp => AppId == SteamCloudApp.AppId;
        private string Name => IsMainApp
            ? $"{SteamCloudApp.Name} ({SteamCloudApp.AppId})"
            : $"referenced app {AppId}";
        private string OwnershipHint => IsMainApp
            ? "; ownership/session may be invalid"
            : "";

        internal static ProductInfoApp Main()
            => new(SteamCloudApp.AppId);

        internal static ProductInfoApp Create(uint appId)
            => new(appId);

        internal async Task<ulong> GetAccessTokenAsync(DepotDownloader owner)
        {
            var token = await owner._connection.GetAppAccessTokenOrPublicAsync(
                AppId,
                AccessTokenDenied()
            );
            if (token == 0)
                owner.Log(PublicAccessTokenFallback());

            return token;
        }

        internal async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo?> FetchInfoAsync(
            DepotDownloader owner
        )
            => await owner._connection.GetAppInfoAsync(
                AppId,
                await GetAccessTokenAsync(owner)
            );

        internal async Task<SteamApps.PICSProductInfoCallback.PICSProductInfo> GetRequiredInfoAsync(
            DepotDownloader owner
        )
        {
            var appInfo = await owner.GetAppInfoAsync(this);
            if (appInfo == null)
                throw new System.Exception(AppInfoUnavailable());

            return appInfo;
        }

        internal KeyValue GetDepotsSection(
            SteamApps.PICSProductInfoCallback.PICSProductInfo appInfo
        )
        {
            var depots = appInfo?.KeyValues?["depots"];
            if (depots == null || depots == KeyValue.Invalid)
                throw new System.InvalidOperationException(MissingDepotsSection());

            return depots;
        }

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
}
