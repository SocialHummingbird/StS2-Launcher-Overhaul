using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudHttpRequestTarget
    {
        internal CloudHttpRequestTarget(
            HttpMethod method,
            bool useHttps,
            string host,
            string path
        )
        {
            Method = method;
            UseHttps = useHttps;
            Host = host;
            Path = path;
        }

        internal HttpMethod Method { get; }
        private bool UseHttps { get; }
        private string Host { get; }
        private string Path { get; }

        internal string Url()
            => $"{(UseHttps ? "https" : "http")}://{Host}{Path}";
    }

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

    private static HttpRequestMessage CreateCloudHttpRequest(CloudHttpRequestTarget target)
        => new(target.Method, target.Url());

    private static void AddCloudHttpHeader(
        HttpRequestMessage request,
        string name,
        string value
    )
        => request.Headers.TryAddWithoutValidation(name, value);

}
