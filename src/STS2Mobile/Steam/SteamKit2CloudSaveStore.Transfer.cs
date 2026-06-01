using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    string ISaveStore.ReadFile(string path)
        => ReadFileCore(path);

    Task<string> ISaveStore.ReadFileAsync(string path)
        => ReadFileAsyncCore(path);

    private string ReadFileCore(string path)
        => ReadFileAsyncCore(path).GetAwaiter().GetResult();

    private async Task<string> ReadFileAsyncCore(string path)
    {
        path = CloudSavePath.Canonicalize(path);

        if (!_cache.FileExists(path))
            throw new FileNotFoundException($"Cloud file not found: {path}");

        if (_cache.GetFileSize(path) == 0)
            return string.Empty;

        var result = await _connection
            .SendCloud<CCloud_ClientFileDownload_Request, CCloud_ClientFileDownload_Response>(
                "ClientFileDownload",
                CreateFileDownloadRequest(path)
            )
            .ConfigureAwait(false);

        if (result.appid != SteamCloudApp.AppId || string.IsNullOrEmpty(result.url_host))
            throw new InvalidOperationException($"Cloud download failed for {path}");

        using var httpRequest = CreateFileDownloadHttpRequest(result);
        var data = await ReadCloudHttpBytesAsync(httpRequest).ConfigureAwait(false);
        PatchHelper.Log(Downloaded(
            path,
            data.Length,
            result.encrypted,
            result.file_size,
            result.raw_file_size));

        if (ShouldDecompressDownloadedFile(result, data))
        {
            var compressedSize = data.Length;
            data = DecompressCloudFile(data);
            PatchHelper.Log(Unzipped(path, compressedSize, data.Length));
        }

        return Encoding.UTF8.GetString(data);
    }

    private static CCloud_ClientFileDownload_Request CreateFileDownloadRequest(string path)
        => new() { appid = SteamCloudApp.AppId, filename = path };

    private static HttpRequestMessage CreateFileDownloadHttpRequest(
        CCloud_ClientFileDownload_Response result
    )
    {
        var request = CreateCloudHttpRequest(
            HttpMethod.Get,
            result.use_https,
            result.url_host,
            result.url_path
        );

        foreach (var header in result.request_headers)
            AddCloudHttpHeader(request, header.name, header.value);

        return request;
    }

    private static bool ShouldDecompressDownloadedFile(
        CCloud_ClientFileDownload_Response result,
        byte[] data
    )
        => result.raw_file_size > 0
            && result.raw_file_size != result.file_size
            && HasZipMagic(data);

    private static bool HasZipMagic(byte[] data)
        => data.Length >= 4
            && data[0] == 0x50
            && data[1] == 0x4B
            && data[2] == 0x03
            && data[3] == 0x04;
}
