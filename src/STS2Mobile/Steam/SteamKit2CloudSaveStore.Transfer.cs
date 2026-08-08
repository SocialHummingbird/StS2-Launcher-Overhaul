using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using SteamKit2.Internal;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
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

        internal byte[] ReadBytes()
            => ContentBytes();

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
        => ReadFileAsyncCore(path, CancellationToken.None);

    Task<string> ICancellableSaveStore.ReadFileAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileAsyncCore(path, cancellationToken);

    Task<byte[]> IRawSaveStore.ReadFileBytesAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileBytesAsyncCore(path, cancellationToken);

    Task<string> ITransferSaveStore.ReadFileForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileAsyncCore(
            path,
            cancellationToken,
            bypassCache: true
        );

    Task<byte[]> ITransferSaveStore.ReadFileBytesForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileBytesAsyncCore(
            path,
            cancellationToken,
            bypassCache: true
        );

    Task<bool> ITransferSaveStore.FileExistsForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    )
        => RemoteFileExistsAsync(path, cancellationToken);

    private string ReadFileCore(string path)
        => ReadFileAsyncCore(path, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

    private async Task<string> ReadFileAsyncCore(
        string path,
        CancellationToken cancellationToken,
        bool bypassCache = false
    )
        => Encoding.UTF8.GetString(
            await ReadFileBytesAsyncCore(
                path,
                cancellationToken,
                bypassCache
            ).ConfigureAwait(false)
        );

    private async Task<byte[]> ReadFileBytesAsyncCore(
        string path,
        CancellationToken cancellationToken,
        bool bypassCache = false
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        path = CloudSavePath.Canonicalize(path);
        var cacheLoaded = !bypassCache && _cache.IsLoaded();
        PatchHelper.Log(
            $"[Cloud] Read: starting {path} cacheLoaded={cacheLoaded} bypassCache={bypassCache}"
        );

        if (cacheLoaded && !_cache.FileExists(path))
            throw new FileNotFoundException($"Cloud file not found: {path}");

        if (cacheLoaded && _cache.GetFileSize(path) == 0)
        {
            PatchHelper.Log($"[Cloud] Read: cache says empty {path}");
            return Array.Empty<byte>();
        }

        var result = await RequestFileDownloadAsync(
            path,
            cancellationToken
        ).ConfigureAwait(false);

        PatchHelper.Log(
            $"[Cloud] Read: download URL received for {path} host={(string.IsNullOrEmpty(result.url_host) ? "<none>" : result.url_host)} fileSize={result.file_size} rawSize={result.raw_file_size} encrypted={result.encrypted}"
        );
        ValidateRemoteFileResponse(path, result);
        if (result.file_size == 0 && result.raw_file_size == 0)
            return Array.Empty<byte>();

        var download = CloudFileDownload.FromValidated(path, result);
        using var httpRequest = download.CreateHttpRequest();
        PatchHelper.Log($"[Cloud] Read: fetching bytes for {path}");
        var data = await ReadCloudHttpBytesAsync(
            httpRequest,
            cancellationToken
        ).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        PatchHelper.Log($"[Cloud] Read: fetched {data.Length} bytes for {path}");
        var content = download.ReadBytes(data);
        cancellationToken.ThrowIfCancellationRequested();
        return content;
    }

    private async Task<bool> RemoteFileExistsAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        path = CloudSavePath.Canonicalize(path);
        try
        {
            var result = await RequestFileDownloadAsync(
                path,
                cancellationToken
            ).ConfigureAwait(false);
            ValidateRemoteFileResponse(path, result);
            return true;
        }
        catch (Exception ex) when (IsRemoteFileMissing(ex))
        {
            return false;
        }
    }

    private async Task<CCloud_ClientFileDownload_Response> RequestFileDownloadAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        PatchHelper.Log($"[Cloud] Read: requesting download URL for {path}");
        return await _connection
            .SendCloud<CCloud_ClientFileDownload_Request, CCloud_ClientFileDownload_Response>(
                "ClientFileDownload",
                CreateFileDownloadRequest(path),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    private static void ValidateRemoteFileResponse(
        string path,
        CCloud_ClientFileDownload_Response result
    )
    {
        if (result.appid != SteamCloudApp.AppId)
            throw new InvalidOperationException($"Cloud download failed for {path}");

        if (
            (result.file_size > 0 || result.raw_file_size > 0)
            && string.IsNullOrEmpty(result.url_host)
        )
        {
            throw new InvalidOperationException($"Cloud download failed for {path}");
        }
    }

    private static bool IsRemoteFileMissing(Exception ex)
        => ex is FileNotFoundException
            || ex.Message.Contains(
                "FileNotFound",
                StringComparison.OrdinalIgnoreCase
            );
}
