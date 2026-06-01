using System;
using System.Net.Http;
using System.Text.Json;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private sealed class ParsedBridgeResponse : IDisposable
    {
        private readonly JsonDocument _document;

        internal ParsedBridgeResponse(JsonDocument document, int status)
        {
            _document = document;
            Status = status;
        }

        internal JsonElement Root => _document.RootElement;
        internal int Status { get; }

        public void Dispose()
            => _document.Dispose();
    }

    private static ParsedBridgeResponse ReadBridgeResponse(
        string? raw,
        Uri? requestUri,
        string requestDescription
    )
    {
        var bridgeResponse = RequireBridgeResponse(raw, requestDescription);
        var doc = ParseBridgeResponse(bridgeResponse, requestDescription);
        try
        {
            ThrowIfBridgeError(doc.RootElement, requestUri, requestDescription);
            return new ParsedBridgeResponse(
                doc,
                ReadStatusCode(doc.RootElement, requestDescription)
            );
        }
        catch
        {
            doc.Dispose();
            throw;
        }
    }

    private static string RequireBridgeResponse(
        string? raw,
        string requestDescription
    )
    {
        if (!string.IsNullOrEmpty(raw))
            return raw;

        throw new HttpRequestException(
            $"Android Java HTTP bridge returned an empty response for {requestDescription}"
        );
    }

    private static JsonDocument ParseBridgeResponse(string raw, string requestDescription)
    {
        try
        {
            return JsonDocument.Parse(raw);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge returned malformed JSON for {requestDescription}",
                ex
            );
        }
    }

    private static void ThrowIfBridgeError(
        JsonElement root,
        Uri? requestUri,
        string requestDescription
    )
    {
        if (!root.TryGetProperty(ErrorProperty, out var error))
            return;

        var errorMessage = SanitizeBridgeError(
            GetBridgeString(error),
            requestUri
        );
        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(errorMessage)
                ? $"Android Java HTTP bridge reported an unknown error for {requestDescription}"
                : $"Android Java HTTP bridge error for {requestDescription}: {errorMessage}"
        );
    }

    private static int ReadStatusCode(JsonElement root, string requestDescription)
    {
        var status = RequireStatusCode(root, requestDescription);
        if (!IsValidHttpStatusCode(status))
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge returned an invalid HTTP status for {requestDescription}: {status}"
            );
        }

        return status;
    }

    private static int RequireStatusCode(JsonElement root, string requestDescription)
    {
        if (
            root.TryGetProperty(StatusProperty, out var statusElement)
            && statusElement.TryGetInt32(out var status)
        )
        {
            return status;
        }

        throw new HttpRequestException(
            $"Android Java HTTP bridge returned a response without a valid status for {requestDescription}"
        );
    }

    private static bool IsValidHttpStatusCode(int status)
        => status is >= 100 and <= 599;
}
