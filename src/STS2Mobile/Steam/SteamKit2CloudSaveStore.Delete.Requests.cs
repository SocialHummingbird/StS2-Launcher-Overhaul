using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private static CCloud_ClientDeleteFile_Request CreateDeleteFileRequest(string path)
        => new()
        {
            appid = SteamCloudApp.AppId,
            filename = path,
        };
}
