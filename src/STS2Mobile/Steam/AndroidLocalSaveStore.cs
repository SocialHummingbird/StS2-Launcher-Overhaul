using System;
using System.IO;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

public sealed class AndroidLocalSaveStore : ISaveStore
{
    private readonly string _basePath;

    public AndroidLocalSaveStore()
    {
        _basePath = OS.GetUserDataDir();
        Directory.CreateDirectory(_basePath);
    }

    public string ReadFile(string path) => File.ReadAllText(GetFullPath(path));

    public Task<string> ReadFileAsync(string path) => Task.FromResult(ReadFile(path));

    public void WriteFile(string path, string content)
    {
        WriteFile(path, System.Text.Encoding.UTF8.GetBytes(content));
    }

    public void WriteFile(string path, byte[] bytes)
    {
        var fullPath = GetFullPath(path);
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        File.WriteAllBytes(fullPath, bytes);
    }

    public Task WriteFileAsync(string path, string content)
    {
        WriteFile(path, content);
        return Task.CompletedTask;
    }

    public Task WriteFileAsync(string path, byte[] bytes)
    {
        WriteFile(path, bytes);
        return Task.CompletedTask;
    }

    public bool FileExists(string path) => File.Exists(GetFullPath(path));

    public bool DirectoryExists(string path) => Directory.Exists(GetFullPath(path));

    public void DeleteFile(string path)
    {
        var fullPath = GetFullPath(path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    public void RenameFile(string sourcePath, string destinationPath)
    {
        var source = GetFullPath(sourcePath);
        var destination = GetFullPath(destinationPath);
        var parent = Path.GetDirectoryName(destination);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);

        File.Move(source, destination, overwrite: true);
    }

    public string[] GetFilesInDirectory(string directoryPath)
    {
        var fullPath = GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            return Array.Empty<string>();

        var files = Directory.GetFiles(fullPath);
        for (var i = 0; i < files.Length; i++)
            files[i] = Path.GetFileName(files[i]);
        return files;
    }

    public string[] GetDirectoriesInDirectory(string directoryPath)
    {
        var fullPath = GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            return Array.Empty<string>();

        var dirs = Directory.GetDirectories(fullPath);
        for (var i = 0; i < dirs.Length; i++)
            dirs[i] = Path.GetFileName(dirs[i]);
        return dirs;
    }

    public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(GetFullPath(directoryPath));

    public void DeleteDirectory(string directoryPath)
    {
        var fullPath = GetFullPath(directoryPath);
        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive: true);
    }

    public void DeleteTemporaryFiles(string directoryPath)
    {
        var fullPath = GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            return;

        foreach (var file in Directory.GetFiles(fullPath, "*.tmp"))
            File.Delete(file);
    }

    public DateTimeOffset GetLastModifiedTime(string path)
    {
        var fullPath = GetFullPath(path);
        return File.Exists(fullPath)
            ? new DateTimeOffset(File.GetLastWriteTimeUtc(fullPath), TimeSpan.Zero)
            : DateTimeOffset.MinValue;
    }

    public int GetFileSize(string path)
    {
        var fullPath = GetFullPath(path);
        return File.Exists(fullPath) ? checked((int)new FileInfo(fullPath).Length) : 0;
    }

    public void SetLastModifiedTime(string path, DateTimeOffset time)
    {
        var fullPath = GetFullPath(path);
        if (File.Exists(fullPath) && time > DateTimeOffset.MinValue)
            File.SetLastWriteTimeUtc(fullPath, time.UtcDateTime);
    }

    public string GetFullPath(string filename)
    {
        var canonical = (filename ?? string.Empty)
            .Replace("user://", string.Empty)
            .Replace('\\', '/')
            .TrimStart('/');

        return Path.GetFullPath(Path.Combine(_basePath, canonical));
    }
}
