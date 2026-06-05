using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidJavaHttpMessageHandler
{
    private readonly struct BridgeResponseContentContext
    {
        internal BridgeResponseContentContext(
            int status,
            BridgeRequestContext request
        )
        {
            Status = status;
            Request = request;
        }

        internal int Status { get; }
        internal BridgeRequestContext Request { get; }
        internal string RequestDescription => Request.Description;
    }

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
            BridgeResponseContentContext context,
            CancellationToken cancellationToken
        )
            => HasFile
                ? CreateResponseContentFromBodyFile(
                    FilePath,
                    context.Status,
                    context.RequestDescription,
                    cancellationToken
                )
                : CreateResponseContentFromBase64Body(Root, context.RequestDescription);
    }

    private static HttpContent CreateResponseContent(
        JsonElement root,
        int status,
        BridgeRequestContext requestContext,
        CancellationToken cancellationToken
    )
        => BridgeResponseBody
            .From(root)
            .CreateContent(
                new BridgeResponseContentContext(status, requestContext),
                cancellationToken
            );

    private static string? ReadBodyFilePath(JsonElement root)
        => TryGetBridgeString(root, BodyFileProperty, out var bodyFilePath)
            ? bodyFilePath
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
        return TryGetBridgeString(root, BodyProperty, out var body)
            ? body ?? string.Empty
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
