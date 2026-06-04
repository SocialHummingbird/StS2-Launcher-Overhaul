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

    private static bool TryGetBridgeString(
        JsonElement root,
        string property,
        out string? value
    )
    {
        if (root.TryGetProperty(property, out var element))
        {
            value = GetBridgeString(element);
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryGetBridgeInt32(
        JsonElement root,
        string property,
        out int value
    )
    {
        if (
            root.TryGetProperty(property, out var element)
            && element.TryGetInt32(out value)
        )
            return true;

        value = default;
        return false;
    }

    private static bool TryGetBridgeObject(
        JsonElement root,
        string property,
        out JsonElement value
    )
    {
        if (
            root.TryGetProperty(property, out value)
            && value.ValueKind == JsonValueKind.Object
        )
            return true;

        value = default;
        return false;
    }

    private static bool TryGetBridgeArray(JsonElement element, out JsonElement array)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            array = element;
            return true;
        }

        array = default;
        return false;
    }
}
