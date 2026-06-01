using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private async Task<byte[]> ReadCloudHttpBytesAsync(HttpRequestMessage request)
    {
        using var response = await SendCloudHttpRequestAsync(request).ConfigureAwait(false);
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    private async Task SendCloudHttpAsync(HttpRequestMessage request)
    {
        using var response = await SendCloudHttpRequestAsync(request).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendCloudHttpRequestAsync(HttpRequestMessage request)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var response = await _http.SendAsync(request, cts.Token).ConfigureAwait(false);
        try
        {
            response.EnsureSuccessStatusCode();
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    private static string CloudHttpUrl(bool useHttps, string host, string path)
        => $"{(useHttps ? "https" : "http")}://{host}{path}";
}
