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

        var download = CloudFileDownload.FromValidated(path, result);
        using var httpRequest = download.CreateHttpRequest();
        var data = await ReadCloudHttpBytesAsync(httpRequest).ConfigureAwait(false);
        return download.ReadText(data);
    }
}
