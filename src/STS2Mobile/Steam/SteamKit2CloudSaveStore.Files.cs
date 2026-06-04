using System;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
    private readonly struct CloudFileWriteRequest
    {
        private CloudFileWriteRequest(
            string canonPath,
            byte[] bytes,
            DateTimeOffset timestamp
        )
        {
            CanonPath = canonPath;
            Bytes = bytes;
            Timestamp = timestamp;
        }

        private string CanonPath { get; }
        private byte[] Bytes { get; }
        private DateTimeOffset Timestamp { get; }

        internal static CloudFileWriteRequest From(string path, byte[] bytes)
            => new(
                CloudSavePath.Canonicalize(path),
                bytes,
                TruncatedUtcNow()
            );

        internal void Apply(SteamKit2CloudSaveStore store)
        {
            store._cache.Set(CanonPath, Bytes.Length, Timestamp);

            if (store._saveBatch.TryCollect(CanonPath, Bytes))
                return;

            store.EnqueueUpload(CanonPath, Bytes, Timestamp);
        }

        private static DateTimeOffset TruncatedUtcNow()
            => DateTimeOffset.FromUnixTimeSeconds(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            );
    }

    private readonly struct CloudFileDeleteRequest
    {
        private CloudFileDeleteRequest(string canonPath)
        {
            CanonPath = canonPath;
        }

        private string CanonPath { get; }

        internal static CloudFileDeleteRequest From(string path)
            => new(CloudSavePath.Canonicalize(path));

        internal void Apply(SteamKit2CloudSaveStore store)
        {
            store._cache.Remove(CanonPath);
            store.EnqueueDelete(CanonPath);
        }
    }

    void ISaveStore.WriteFile(string path, string content)
        => WriteFileCore(path, content);

    void ISaveStore.WriteFile(string path, byte[] bytes)
        => WriteFileCore(path, bytes);

    Task ISaveStore.WriteFileAsync(string path, string content)
    {
        WriteFileCore(path, content);
        return Task.CompletedTask;
    }

    Task ISaveStore.WriteFileAsync(string path, byte[] bytes)
    {
        WriteFileCore(path, bytes);
        return Task.CompletedTask;
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
        try
        {
            DeleteFileCore(sourcePath);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                RenameDeleteFailed(
                    CloudSavePath.Canonicalize(sourcePath),
                    ex
                )
            );
        }
    }

    private void WriteFileCore(string path, string content)
        => WriteFileCore(path, Encoding.UTF8.GetBytes(content));

    private void WriteFileCore(string path, byte[] bytes)
        => CloudFileWriteRequest.From(path, bytes).Apply(this);

    private void DeleteFileCore(string path)
        => CloudFileDeleteRequest.From(path).Apply(this);
}
