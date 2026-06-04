namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private readonly partial struct ProductInfoApp
    {
        private bool IsMainApp => AppId == SteamCloudApp.AppId;
        private string Name => IsMainApp
            ? $"{SteamCloudApp.Name} ({SteamCloudApp.AppId})"
            : $"referenced app {AppId}";
        private string OwnershipHint => IsMainApp
            ? "; ownership/session may be invalid"
            : "";

        private string AccessTokenDenied()
            => $"Steam denied app access token for {Name}{OwnershipHint}";

        private string PublicAccessTokenFallback()
            => $"Steam returned no app access token for {Name}; "
                + "continuing with public token 0";

        private string AppInfoUnavailable()
            => $"Failed to get app info from Steam for {Name}{OwnershipHint}";

        private string MissingDepotsSection()
            => $"Steam app info for {Name} has no depots section";
    }
}
