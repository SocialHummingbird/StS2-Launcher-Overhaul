using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private const string UploadBlockContentType = "application/octet-stream";

    private readonly struct UploadBlockBody
    {
        private UploadBlockBody(byte[] data)
        {
            Data = data;
        }

        internal byte[] Data { get; }

        internal static UploadBlockBody From(
            byte[]? explicitBodyData,
            byte[] uploadBytes,
            int offset,
            int length
        )
        {
            if (explicitBodyData is { Length: > 0 })
                return new UploadBlockBody(explicitBodyData);

            return new UploadBlockBody(SliceUploadBlock(uploadBytes, offset, length));
        }
    }

    private readonly struct UploadBlockRequest
    {
        private UploadBlockRequest(
            HttpMethod method,
            bool useHttps,
            string host,
            string path,
            UploadBlockBody body
        )
        {
            Method = method;
            UseHttps = useHttps;
            Host = host;
            Path = path;
            Body = body;
        }

        private HttpMethod Method { get; }
        private bool UseHttps { get; }
        private string Host { get; }
        private string Path { get; }
        private UploadBlockBody Body { get; }

        internal static UploadBlockRequest Create(
            HttpMethod method,
            bool useHttps,
            string host,
            string path,
            UploadBlockBody body
        )
            => new(method, useHttps, host, path, body);

        internal HttpRequestMessage CreateHttpRequest()
            => CreateUploadBlockRequest(
                Method,
                UseHttps,
                Host,
                Path,
                Body.Data
            );
    }

    private async Task SendUploadBlocksAsync(
        CCloud_ClientBeginFileUpload_Response beginResult,
        byte[] uploadBytes
    )
    {
        foreach (var block in beginResult.block_requests)
        {
            var body = UploadBlockBody.From(
                block.explicit_body_data,
                uploadBytes,
                (int)block.block_offset,
                (int)block.block_length
            );
            using var request = UploadBlockRequest
                .Create(
                    block.http_method == 2 ? HttpMethod.Post : HttpMethod.Put,
                    block.use_https,
                    block.url_host,
                    block.url_path,
                    body
                )
                .CreateHttpRequest();
            foreach (var header in block.request_headers)
                AddCloudHttpHeader(request, header.name, header.value);

            await SendCloudHttpAsync(request).ConfigureAwait(false);
        }
    }

    private static HttpRequestMessage CreateUploadBlockRequest(
        HttpMethod method,
        bool useHttps,
        string host,
        string path,
        byte[] bodyData
    )
    {
        var request = CreateCloudHttpRequest(
            new CloudHttpRequestTarget(method, useHttps, host, path)
        );
        request.Content = new ByteArrayContent(bodyData);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(UploadBlockContentType);
        request.Content.Headers.ContentLength = bodyData.Length;
        return request;
    }

    private static byte[] SliceUploadBlock(byte[] uploadBytes, int offset, int length)
        => uploadBytes[offset..(offset + length)];
}
