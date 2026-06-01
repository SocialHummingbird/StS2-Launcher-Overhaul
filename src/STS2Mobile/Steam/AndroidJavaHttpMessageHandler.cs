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

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var app = GetGodotApp();
        if (app == null)
            throw new HttpRequestException("GodotApp Java bridge is unavailable");

        var bodyBytes = await ReadRequestBodyAsync(request, cancellationToken).ConfigureAwait(false);
        var requestDescription = $"{request.Method} {SanitizeUri(request.RequestUri)}";

        cancellationToken.ThrowIfCancellationRequested();

        string? raw = CallHttpRequestBridge(app, request, bodyBytes, requestDescription);

        ThrowIfCancelledWithRawResponseCleanup(raw, cancellationToken);
        using var bridgeResponse = ReadBridgeResponse(raw, request.RequestUri, requestDescription);
        return bridgeResponse.CreateResponseWithContent(
            request,
            requestDescription,
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
