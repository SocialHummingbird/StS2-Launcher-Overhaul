using System;
using System.Text;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal sealed partial class SteamKit2CloudSaveStore
{
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
    {
        var canonPath = CloudSavePath.Canonicalize(path);
        var timestamp = TruncatedUtcNow();
        _cache.Set(canonPath, bytes.Length, timestamp);

        if (_saveBatch.TryCollect(canonPath, bytes))
            return;

        EnqueueUpload(canonPath, bytes, timestamp);
    }

    private void DeleteFileCore(string path)
    {
        var canonPath = CloudSavePath.Canonicalize(path);
        _cache.Remove(canonPath);
        EnqueueDelete(canonPath);
    }

    private static DateTimeOffset TruncatedUtcNow()
        => DateTimeOffset.FromUnixTimeSeconds(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        );
}
