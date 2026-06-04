using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private sealed class ParsedBridgeResponse : IDisposable
    {
        private readonly JsonDocument _document;

        private ParsedBridgeResponse(JsonDocument document, int status)
        {
            _document = document;
            Status = status;
        }

        private JsonElement Root => _document.RootElement;
        private int Status { get; }

        internal HttpResponseMessage CreateResponseWithContent(
            HttpRequestMessage request,
            BridgeRequestContext requestContext,
            CancellationToken cancellationToken
        )
        {
            var response = CreateBridgeResponse(request, Root, Status);
            try
            {
                response.Content = CreateResponseContent(
                    Root,
                    Status,
                    requestContext,
                    cancellationToken
                );
                ApplyBridgeHeaders(response, Root);

                return response;
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }

        public void Dispose()
            => _document.Dispose();

        internal static ParsedBridgeResponse CreateValidated(
            JsonDocument document,
            BridgeRequestContext requestContext
        )
        {
            try
            {
                ThrowIfBridgeError(document.RootElement, requestContext);
                return new(
                    document,
                    ReadStatusCode(document.RootElement, requestContext)
                );
            }
            catch
            {
                document.Dispose();
                throw;
            }
        }
    }

    private static ParsedBridgeResponse ReadBridgeResponse(
        string? raw,
        BridgeRequestContext requestContext
    )
    {
        var bridgeResponse = RequireBridgeResponse(raw, requestContext.Description);
        var doc = ParseBridgeResponse(bridgeResponse, requestContext.Description);
        return ParsedBridgeResponse.CreateValidated(
            doc,
            requestContext
        );
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
        BridgeRequestContext requestContext
    )
    {
        if (!TryGetBridgeString(root, ErrorProperty, out var error))
            return;

        var errorMessage = SanitizeBridgeError(
            error,
            requestContext.Uri
        );
        throw new HttpRequestException(
            string.IsNullOrWhiteSpace(errorMessage)
                ? $"Android Java HTTP bridge reported an unknown error for {requestContext.Description}"
                : $"Android Java HTTP bridge error for {requestContext.Description}: {errorMessage}"
        );
    }

    private static int ReadStatusCode(JsonElement root, BridgeRequestContext requestContext)
    {
        var status = RequireStatusCode(root, requestContext);
        if (!IsValidHttpStatusCode(status))
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge returned an invalid HTTP status for {requestContext.Description}: {status}"
            );
        }

        return status;
    }

    private static int RequireStatusCode(JsonElement root, BridgeRequestContext requestContext)
    {
        if (TryGetBridgeInt32(root, StatusProperty, out var status))
            return status;

        throw new HttpRequestException(
            $"Android Java HTTP bridge returned a response without a valid status for {requestContext.Description}"
        );
    }

    private static bool IsValidHttpStatusCode(int status)
        => status is >= 100 and <= 599;
}
