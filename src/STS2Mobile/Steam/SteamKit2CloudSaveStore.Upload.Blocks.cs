using System.Net.Http;
using System.Threading.Tasks;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private async Task SendUploadBlocksAsync(
        CCloud_ClientBeginFileUpload_Response beginResult,
        byte[] uploadBytes
    )
    {
        foreach (var block in beginResult.block_requests)
        {
            var method = block.http_method == 2 ? HttpMethod.Post : HttpMethod.Put;
            using var request = new HttpRequestMessage(
                method,
                CloudHttpUrl(block.use_https, block.url_host, block.url_path));

            byte[] bodyData =
                block.explicit_body_data?.Length > 0
                    ? block.explicit_body_data
                    : uploadBytes[
                        (int)block.block_offset..(
                            (int)block.block_offset + (int)block.block_length
                        )
                    ];
            request.Content = new ByteArrayContent(bodyData);
            request.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            request.Content.Headers.ContentLength = bodyData.Length;

            foreach (var header in block.request_headers)
                request.Headers.TryAddWithoutValidation(header.name, header.value);

            await SendCloudHttpAsync(request).ConfigureAwait(false);
        }
    }
}
