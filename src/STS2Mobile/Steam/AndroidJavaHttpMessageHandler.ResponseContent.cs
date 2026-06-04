using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private readonly struct BridgeResponseBody
    {
        private BridgeResponseBody(JsonElement root, string? filePath)
        {
            Root = root;
            FilePath = filePath;
        }

        private JsonElement Root { get; }
        private string? FilePath { get; }
        private bool HasFile => FilePath != null;

        internal static BridgeResponseBody From(JsonElement root)
            => new(root, ReadBodyFilePath(root));

        internal void DeleteFileIfSafe()
            => DeleteBodyFileIfSafe(FilePath);

        internal HttpContent CreateContent(
            int status,
            string requestDescription,
            CancellationToken cancellationToken
        )
            => HasFile
                ? CreateResponseContentFromBodyFile(
                    FilePath,
                    status,
                    requestDescription,
                    cancellationToken
                )
                : CreateResponseContentFromBase64Body(Root, requestDescription);
    }

    private static HttpContent CreateResponseContent(
        JsonElement root,
        int status,
        BridgeRequestContext requestContext,
        CancellationToken cancellationToken
    )
        => BridgeResponseBody
            .From(root)
            .CreateContent(status, requestContext.Description, cancellationToken);

    private static string? ReadBodyFilePath(JsonElement root)
        => root.TryGetProperty(BodyFileProperty, out var bodyFileElement)
            ? GetBridgeString(bodyFileElement)
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
            BridgeResponseBody.From(doc.RootElement).DeleteFileIfSafe();
        }
        catch
        {
        }
    }
}
