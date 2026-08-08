using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal partial class SteamKit2CloudSaveStore
{
    void ISaveStore.WriteFile(string path, string content)
        => WriteFileCore(path, content);

    void ISaveStore.WriteFile(string path, byte[] bytes)
        => WriteFileCore(path, bytes);

    Task ISaveStore.WriteFileAsync(string path, string content)
        => WriteFileCancellableAsync(
            path,
            Encoding.UTF8.GetBytes(content),
            CancellationToken.None
        );

    Task ISaveStore.WriteFileAsync(string path, byte[] bytes)
        => WriteFileCancellableAsync(
            path,
            bytes,
            CancellationToken.None
        );

    Task ICancellableSaveStore.WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken
    )
        => WriteFileCancellableAsync(
            path,
            Encoding.UTF8.GetBytes(content),
            cancellationToken
        );

    Task IRawSaveStore.WriteFileBytesAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
        => WriteFileCancellableAsync(path, content, cancellationToken);

    async Task ITransferSaveStore.DeleteFileAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var canonPath = CloudSavePath.Canonicalize(path);
        await DeleteCloudFileAsync(
            canonPath,
            cancellationToken
        ).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        _cache.Remove(canonPath);
    }

    bool ISaveStore.FileExists(string path)
        => _cache.FileExists(path);

    bool ISaveStore.DirectoryExists(string path)
        => true;

    void ISaveStore.DeleteFile(string path)
        => DeleteFileCore(path);

    void ISaveStore.RenameFile(string sourcePath, string destinationPath)
    {
        var content = ReadFileCore(sourcePath);
        WriteFileCore(destinationPath, content);
        DeleteFileCore(sourcePath);
    }

    private void WriteFileCore(string path, string content)
        => WriteFileCore(path, Encoding.UTF8.GetBytes(content));

    private void WriteFileCore(string path, byte[] bytes)
        => WriteFileCancellableAsync(
                path,
                bytes,
                CancellationToken.None
            )
            .GetAwaiter()
            .GetResult();

    private async Task WriteFileCancellableAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var canonPath = CloudSavePath.Canonicalize(path);
        var timestamp = TruncatedUtcNow();
        await UploadWithRetryAsync(
            canonPath,
            bytes,
            timestamp,
            cancellationToken
        ).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        _cache.Set(canonPath, bytes.Length, timestamp);
    }

    private void DeleteFileCore(string path)
        => ((ITransferSaveStore)this)
            .DeleteFileAsync(path, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

    private static DateTimeOffset TruncatedUtcNow()
        => DateTimeOffset.FromUnixTimeSeconds(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
}
