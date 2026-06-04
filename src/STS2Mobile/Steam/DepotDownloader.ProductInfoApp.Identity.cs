namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly partial struct ProductInfoApp
    {
        private readonly struct ProductInfoAppIdentity
        {
            internal static ProductInfoAppIdentity For(uint appId)
                => new(appId);

            private ProductInfoAppIdentity(uint appId)
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
}
