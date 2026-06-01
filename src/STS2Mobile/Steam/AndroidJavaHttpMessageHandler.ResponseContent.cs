using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private static HttpContent CreateResponseContent(
        JsonElement root,
        int status,
        string requestDescription,
        CancellationToken cancellationToken
    )
    {
        var bodyFileElement = GetBodyFileElement(root);
        if (bodyFileElement.HasValue)
            return CreateResponseContentFromBodyFile(
                bodyFileElement.Value,
                status,
                requestDescription,
                cancellationToken
            );

        return CreateResponseContentFromBase64Body(root, requestDescription);
    }

    private static JsonElement? GetBodyFileElement(JsonElement root)
        => root.TryGetProperty(BodyFileProperty, out var bodyFileElement)
            ? bodyFileElement
            : null;

    private static HttpContent CreateResponseContentFromBase64Body(
        JsonElement root,
        string requestDescription
    )
    {
        try
        {
            return new ByteArrayContent(
                Convert.FromBase64String(ReadBase64ResponseBody(root))
            );
        }
        catch (FormatException ex)
        {
            throw new HttpRequestException(
                $"Android Java HTTP bridge returned malformed base64 response body for {requestDescription}",
                ex
            );
        }
    }

    private static string ReadBase64ResponseBody(JsonElement root)
    {
        return root.TryGetProperty(BodyProperty, out var bodyElement)
            ? GetBridgeString(bodyElement) ?? string.Empty
            : string.Empty;
    }

    private static void DeleteBodyFileFromRawResponse(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var bodyFileElement = GetBodyFileElement(doc.RootElement);
            if (bodyFileElement.HasValue)
                DeleteBodyFileIfSafe(bodyFileElement.Value.GetString());
        }
        catch
        {
        }
    }
}
