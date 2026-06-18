using System;
using System.Net.Http;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudFileDownload
    {
        private CloudFileDownload(string path, CCloud_ClientFileDownload_Response result)
        {
            Path = path;
            Result = result;
        }

        private string Path { get; }
        private CCloud_ClientFileDownload_Response Result { get; }

        internal static CloudFileDownload FromValidated(
            string path,
            CCloud_ClientFileDownload_Response result
        )
        {
            if (result.appid == SteamCloudApp.AppId && !string.IsNullOrEmpty(result.url_host))
                return new CloudFileDownload(path, result);

            throw new InvalidOperationException($"Cloud download failed for {path}");
        }

        internal HttpRequestMessage CreateHttpRequest()
        {
            var request = CreateCloudHttpRequest(
                new CloudHttpRequestTarget(
                    HttpMethod.Get,
                    Result.use_https,
                    Result.url_host,
                    Result.url_path
                )
            );

            foreach (var header in Result.request_headers)
                AddCloudHttpHeader(request, header.name, header.value);

            return request;
        }

        internal string ReadText(byte[] data)
            => CloudDownloadedFile.From(Path, Result, data).ReadText();
    }

    private static CCloud_ClientFileDownload_Request CreateFileDownloadRequest(string path)
        => new() { appid = SteamCloudApp.AppId, filename = path };
}
