using System.Net.Http;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private static CCloud_ClientFileDownload_Request CreateFileDownloadRequest(string path)
        => new() { appid = SteamCloudApp.AppId, filename = path };

    private static HttpRequestMessage CreateFileDownloadHttpRequest(
        CCloud_ClientFileDownload_Response result
    )
    {
        var request = CreateCloudHttpRequest(
            HttpMethod.Get,
            result.use_https,
            result.url_host,
            result.url_path
        );

        foreach (var header in result.request_headers)
            AddCloudHttpHeader(request, header.name, header.value);

        return request;
    }
}
