using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

// SteamKit's default HttpClient uses System.Net.Security.SslStream. In the
// Godot Android Mono runtime, creating that AndroidCrypto SSL stream aborts the
// whole process. Route SteamKit WebAPI/CDN HTTPS requests through Android's Java
// HttpsURLConnection instead.
internal sealed partial class AndroidJavaHttpMessageHandler : HttpMessageHandler
{
    private const int DefaultTimeoutMs = 30_000;
    private const int MaxBufferedErrorBodyBytes = 1024 * 1024;
    private const string BodyProperty = "body";
    private const string BodyFileProperty = "bodyFile";
    private const string ErrorProperty = "error";
    private const string HeadersProperty = "headers";
    private const string HttpRequestBridgeMethod = "httpRequest";
    private const string ReasonProperty = "reason";
    private const string StatusProperty = "status";

    private readonly int _timeoutMs;

    private AndroidJavaHttpMessageHandler(int timeoutMs)
    {
        _timeoutMs = timeoutMs;
    }

    private readonly struct BridgeRequestContext
    {
        internal BridgeRequestContext(HttpRequestMessage request)
        {
            Uri = request.RequestUri;
            Description = $"{request.Method} {SanitizeUri(request.RequestUri)}";
        }

        internal Uri? Uri { get; }
        internal string Description { get; }
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var bodyBytes = await ReadRequestBodyAsync(request, cancellationToken).ConfigureAwait(false);
        var requestContext = new BridgeRequestContext(request);

        cancellationToken.ThrowIfCancellationRequested();

        string? raw = CallHttpRequestBridge(request, bodyBytes, requestContext);

        ThrowIfCancelledWithRawResponseCleanup(raw, cancellationToken);
        using var bridgeResponse = ReadBridgeResponse(raw, requestContext);
        return bridgeResponse.CreateResponseWithContent(
            request,
            requestContext,
            cancellationToken
        );
    }

    private static async Task<byte[]> ReadRequestBodyAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        return request.Content == null
            ? Array.Empty<byte>()
            : await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ThrowIfCancelledWithRawResponseCleanup(
        string? raw,
        CancellationToken cancellationToken
    )
    {
        if (!cancellationToken.IsCancellationRequested)
            return;

        DeleteBodyFileFromRawResponse(raw);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static string? GetBridgeString(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }
}
