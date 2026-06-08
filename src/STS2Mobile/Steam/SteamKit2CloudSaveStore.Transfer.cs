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
    private readonly HttpClient _http = OperatingSystem.IsAndroid()
        ? AndroidJavaHttpMessageHandler.CreateCdnClient()
        : new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    private readonly struct CloudDownloadedFile
    {
        private CloudDownloadedFile(string path, CCloud_ClientFileDownload_Response result, byte[] data)
        {
            Path = path;
            Result = result;
            Data = data;
        }

        private string Path { get; }
        private CCloud_ClientFileDownload_Response Result { get; }
        private byte[] Data { get; }

        internal static CloudDownloadedFile From(
            string path,
            CCloud_ClientFileDownload_Response result,
            byte[] data
        )
        {
            PatchHelper.Log(Downloaded(
                path,
                data.Length,
                result.encrypted,
                result.file_size,
                result.raw_file_size
            ));
            return new CloudDownloadedFile(path, result, data);
        }

        internal string ReadText()
            => Encoding.UTF8.GetString(ContentBytes());

        private byte[] ContentBytes()
        {
            if (!ShouldDecompressDownloadedFile(Result, Data))
                return Data;

            var compressedSize = Data.Length;
            var decompressed = DecompressCloudFile(Data);
            PatchHelper.Log(Unzipped(Path, compressedSize, decompressed.Length));
            return decompressed;
        }
    }

    string ISaveStore.ReadFile(string path)
        => ReadFileCore(path);

    Task<string> ISaveStore.ReadFileAsync(string path)
        => ReadFileAsyncCore(path);

    private string ReadFileCore(string path)
        => ReadFileAsyncCore(path).GetAwaiter().GetResult();

    private async Task<string> ReadFileAsyncCore(string path)
    {
        path = CloudSavePath.Canonicalize(path);
        var cacheLoaded = _cache.IsLoaded();
        PatchHelper.Log($"[Cloud] Read: starting {path} cacheLoaded={cacheLoaded}");

        if (cacheLoaded && !_cache.FileExists(path))
            throw new FileNotFoundException($"Cloud file not found: {path}");

        if (cacheLoaded && _cache.GetFileSize(path) == 0)
        {
            PatchHelper.Log($"[Cloud] Read: cache says empty {path}");
            return string.Empty;
        }

        PatchHelper.Log($"[Cloud] Read: requesting download URL for {path}");
        var result = await _connection
            .SendCloud<CCloud_ClientFileDownload_Request, CCloud_ClientFileDownload_Response>(
                "ClientFileDownload",
                CreateFileDownloadRequest(path)
            )
            .ConfigureAwait(false);

        PatchHelper.Log(
            $"[Cloud] Read: download URL received for {path} host={(string.IsNullOrEmpty(result.url_host) ? "<none>" : result.url_host)} fileSize={result.file_size} rawSize={result.raw_file_size} encrypted={result.encrypted}"
        );
        var download = CloudFileDownload.FromValidated(path, result);
        using var httpRequest = download.CreateHttpRequest();
        PatchHelper.Log($"[Cloud] Read: fetching bytes for {path}");
        var data = await ReadCloudHttpBytesAsync(httpRequest).ConfigureAwait(false);
        PatchHelper.Log($"[Cloud] Read: fetched {data.Length} bytes for {path}");
        return download.ReadText(data);
    }
}
