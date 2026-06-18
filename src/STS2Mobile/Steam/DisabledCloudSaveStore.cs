using System;
using System.IO;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal class DisabledCloudSaveStore : ICloudSaveStore
{
    private readonly ISaveStore _local;

    internal DisabledCloudSaveStore(ISaveStore local)
    {
        _local = local;
    }

    string ISaveStore.ReadFile(string path)
        => throw Missing(path);

    Task<string> ISaveStore.ReadFileAsync(string path)
        => Task.FromException<string>(Missing(path));

    void ISaveStore.WriteFile(string path, string content)
    {
        PatchHelper.Log($"[Cloud] Disabled cloud write ignored: {path}");
    }

    void ISaveStore.WriteFile(string path, byte[] bytes)
    {
        PatchHelper.Log($"[Cloud] Disabled cloud write ignored: {path}");
    }

    Task ISaveStore.WriteFileAsync(string path, string content)
    {
        PatchHelper.Log($"[Cloud] Disabled cloud write ignored: {path}");
        return Task.CompletedTask;
    }

    Task ISaveStore.WriteFileAsync(string path, byte[] bytes)
    {
        PatchHelper.Log($"[Cloud] Disabled cloud write ignored: {path}");
        return Task.CompletedTask;
    }

    bool ISaveStore.FileExists(string path)
        => false;

    void ISaveStore.DeleteFile(string path)
    {
        PatchHelper.Log($"[Cloud] Disabled cloud delete ignored: {path}");
    }

    void ISaveStore.RenameFile(string sourcePath, string destinationPath)
    {
        PatchHelper.Log($"[Cloud] Disabled cloud rename ignored: {sourcePath} -> {destinationPath}");
    }

    DateTimeOffset ISaveStore.GetLastModifiedTime(string path)
        => DateTimeOffset.MinValue;

    int ISaveStore.GetFileSize(string path)
        => 0;

    void ISaveStore.SetLastModifiedTime(string path, DateTimeOffset time)
    {
    }

    string ISaveStore.GetFullPath(string filename)
        => _local.GetFullPath(filename);

    bool ISaveStore.DirectoryExists(string path)
        => false;

    string[] ISaveStore.GetFilesInDirectory(string directoryPath)
        => Array.Empty<string>();

    string[] ISaveStore.GetDirectoriesInDirectory(string directoryPath)
        => Array.Empty<string>();

    void ISaveStore.CreateDirectory(string directoryPath)
    {
    }

    void ISaveStore.DeleteDirectory(string directoryPath)
    {
        PatchHelper.Log($"[Cloud] Disabled cloud directory delete ignored: {directoryPath}");
    }

    void ISaveStore.DeleteTemporaryFiles(string directoryPath)
    {
    }

    void ICloudSaveStore.BeginSaveBatch()
    {
    }

    void ICloudSaveStore.EndSaveBatch()
    {
    }

    bool ICloudSaveStore.HasCloudFiles()
        => false;

    void ICloudSaveStore.ForgetFile(string path)
    {
    }

    bool ICloudSaveStore.IsFilePersisted(string path)
        => false;

    public virtual bool HasUserEnabledCloudSync()
        => false;

    private static FileNotFoundException Missing(string path)
        => new($"Cloud sync is disabled for Android local-only saves: {path}");

}
