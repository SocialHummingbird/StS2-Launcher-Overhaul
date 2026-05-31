using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal sealed class AndroidLocalSaveStore : ISaveStore
{
    private readonly string _basePath;
    private readonly string _basePathWithSeparator;

    internal AndroidLocalSaveStore()
    {
        _basePath = Path.GetFullPath(OS.GetUserDataDir());
        _basePathWithSeparator = _basePath.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? _basePath
            : _basePath + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_basePath);
    }

    string ISaveStore.ReadFile(string path) => ReadTextFile(path);

    Task<string> ISaveStore.ReadFileAsync(string path) => Task.FromResult(ReadTextFile(path));

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

    bool ISaveStore.FileExists(string path) => File.Exists(FullPath(path));

    bool ISaveStore.DirectoryExists(string path) => Directory.Exists(FullPath(path));

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

    string[] ISaveStore.GetFilesInDirectory(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            return Array.Empty<string>();

        return Directory.GetFiles(fullPath).Select(Path.GetFileName).ToArray();
    }

    string[] ISaveStore.GetDirectoriesInDirectory(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            return Array.Empty<string>();

        return Directory.GetDirectories(fullPath).Select(Path.GetFileName).ToArray();
    }

    void ISaveStore.CreateDirectory(string directoryPath) => Directory.CreateDirectory(FullPath(directoryPath));

    void ISaveStore.DeleteDirectory(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (Directory.Exists(fullPath))
            Directory.Delete(fullPath, recursive: true);
    }

    void ISaveStore.DeleteTemporaryFiles(string directoryPath)
    {
        var fullPath = FullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            return;

        foreach (var file in Directory.GetFiles(fullPath, "*.tmp"))
            File.Delete(file);
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

    private string ReadTextFile(string path) => File.ReadAllText(FullPath(path));

    private void WriteTextFile(string path, string content)
    {
        WriteBytesFile(path, System.Text.Encoding.UTF8.GetBytes(content));
    }

    private void WriteBytesFile(string path, byte[] bytes)
    {
        var fullPath = FullPath(path);
        EnsureParentDirectory(fullPath);

        File.WriteAllBytes(fullPath, bytes);
    }

    private string FullPath(string path)
    {
        var canonical = (path ?? string.Empty)
            .Replace("user://", string.Empty)
            .Replace('\\', '/')
            .TrimStart('/');
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, canonical));
        if (!IsInsideBasePath(fullPath))
            throw new IOException($"Save path escapes app data directory: {path}");

        return fullPath;
    }

    private void EnsureParentDirectory(string fullPath)
    {
        var parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parent))
            Directory.CreateDirectory(parent);
    }

    private bool IsInsideBasePath(string fullPath)
    {
        return string.Equals(fullPath, _basePath, StringComparison.Ordinal)
            || fullPath.StartsWith(_basePathWithSeparator, StringComparison.Ordinal);
    }
}
