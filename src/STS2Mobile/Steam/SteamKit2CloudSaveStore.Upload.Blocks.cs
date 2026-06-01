using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private const string UploadBlockContentType = "application/octet-stream";

    private async Task SendUploadBlocksAsync(
        CCloud_ClientBeginFileUpload_Response beginResult,
        byte[] uploadBytes
    )
    {
        foreach (var block in beginResult.block_requests)
        {
            byte[] bodyData =
                block.explicit_body_data?.Length > 0
                    ? block.explicit_body_data
                    : SliceUploadBlock(
                        uploadBytes,
                        (int)block.block_offset,
                        (int)block.block_length
                    );
            using var request = CreateUploadBlockRequest(
                block.http_method == 2 ? HttpMethod.Post : HttpMethod.Put,
                block.use_https,
                block.url_host,
                block.url_path,
                bodyData
            );
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
        var request = CreateCloudHttpRequest(method, useHttps, host, path);
        request.Content = new ByteArrayContent(bodyData);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(UploadBlockContentType);
        request.Content.Headers.ContentLength = bodyData.Length;
        return request;
    }

    private static byte[] SliceUploadBlock(byte[] uploadBytes, int offset, int length)
        => uploadBytes[offset..(offset + length)];
}
