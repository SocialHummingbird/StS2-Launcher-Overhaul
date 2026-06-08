using System;
using System.IO;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidLocalSaveStore
{
    string ISaveStore.ReadFile(string path)
    {
        PatchHelper.Log($"[Cloud] Android local save read: {path} -> {FullPath(path)}");
        return ReadTextFile(path);
    }

    Task<string> ISaveStore.ReadFileAsync(string path)
    {
        PatchHelper.Log($"[Cloud] Android local save read async: {path} -> {FullPath(path)}");
        return Task.FromResult(ReadTextFile(path));
    }

    void ISaveStore.WriteFile(string path, string content)
    {
        WriteTextFile(path, content);
    }

    void ISaveStore.WriteFile(string path, byte[] bytes)
    {
        WriteBytesFile(path, bytes);
    }

    Task ISaveStore.WriteFileAsync(string path, string content)
    {
        WriteTextFile(path, content);
        return Task.CompletedTask;
    }

    Task ISaveStore.WriteFileAsync(string path, byte[] bytes)
    {
        WriteBytesFile(path, bytes);
        return Task.CompletedTask;
    }

    bool ISaveStore.FileExists(string path)
    {
        var fullPath = FullPath(path);
        var exists = File.Exists(fullPath);
        if (exists || VerboseDiagnosticsEnabled)
            PatchHelper.Log($"[Cloud] Android local save exists: {path} -> {fullPath} = {exists}");
        return exists;
    }

    void ISaveStore.DeleteFile(string path)
    {
        var fullPath = FullPath(path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    void ISaveStore.RenameFile(string sourcePath, string destinationPath)
    {
        var source = FullPath(sourcePath);
        var destination = FullPath(destinationPath);
        EnsureParentDirectory(destination);

        File.Move(source, destination, overwrite: true);
    }

    DateTimeOffset ISaveStore.GetLastModifiedTime(string path)
    {
        var fullPath = FullPath(path);
        return File.Exists(fullPath)
            ? new DateTimeOffset(File.GetLastWriteTimeUtc(fullPath), TimeSpan.Zero)
            : DateTimeOffset.MinValue;
    }

    int ISaveStore.GetFileSize(string path)
    {
        var fullPath = FullPath(path);
        return File.Exists(fullPath) ? checked((int)new FileInfo(fullPath).Length) : 0;
    }

    void ISaveStore.SetLastModifiedTime(string path, DateTimeOffset time)
    {
        var fullPath = FullPath(path);
        if (File.Exists(fullPath) && time > DateTimeOffset.MinValue)
            File.SetLastWriteTimeUtc(fullPath, time.UtcDateTime);
    }

    string ISaveStore.GetFullPath(string filename)
    {
        return FullPath(filename);
    }
}
