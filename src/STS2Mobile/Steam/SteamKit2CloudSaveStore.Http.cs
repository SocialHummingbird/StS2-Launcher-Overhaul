using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
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

    private async Task<byte[]> ReadCloudHttpBytesAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendCloudHttpRequestAsync(
            request,
            cancellationToken
        ).ConfigureAwait(false);
        return await response.Content.ReadAsByteArrayAsync(
            cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task SendCloudHttpAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        using var response = await SendCloudHttpRequestAsync(
            request,
            cancellationToken
        ).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendCloudHttpRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        using var timeoutCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
        timeoutCancellation.CancelAfter(TimeSpan.FromSeconds(30));
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(
                request,
                timeoutCancellation.Token
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
            when (
                !cancellationToken.IsCancellationRequested
                && timeoutCancellation.IsCancellationRequested
            )
        {
            throw new TimeoutException(
                "Steam Cloud HTTP transfer timed out after 30 seconds",
                ex
            );
        }
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
