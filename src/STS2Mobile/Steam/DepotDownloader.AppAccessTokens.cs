using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class DepotDownloader
{
    private async Task<ulong> GetProductInfoAccessTokenAsync(uint appId)
    {
        string deniedMessage;
        string publicTokenMessage;
        if (appId == SteamCloudApp.AppId)
        {
            deniedMessage =
                $"Steam denied app access token for {SteamCloudApp.Name} ({SteamCloudApp.AppId}); ownership/session may be invalid";
            publicTokenMessage =
                $"Steam returned no app access token for {SteamCloudApp.Name} ({SteamCloudApp.AppId}); continuing with public token 0";
        }
        else
        {
            deniedMessage = $"Steam denied app access token for referenced app {appId}";
            publicTokenMessage =
                $"Steam returned no app access token for referenced app {appId}; continuing with public token 0";
        }

        var token = await _connection.GetAppAccessTokenOrPublicAsync(appId, deniedMessage);
        if (token == 0)
            Log(publicTokenMessage);

        return token;
    }
}
