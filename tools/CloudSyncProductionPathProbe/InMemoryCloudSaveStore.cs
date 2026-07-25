using System.Collections.Concurrent;
using System.Text;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal sealed class InMemoryCloudSaveStore :
    ISaveStore,
    ICloudSaveStore,
    ICancellableSaveStore,
    ICancellableCloudMetadataStore
{
    private readonly ConcurrentDictionary<string, StoredFile> _files =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> _operations = new();
    private readonly string _name;
    private int _activeOperations;
    private int _readCount;
    private int _writeCount;

    internal InMemoryCloudSaveStore(string name)
    {
        _name = name;
    }

    internal Func<string, CancellationToken, Task>? BeforeReadAsync { get; set; }
    internal Func<string, CancellationToken, Task>? BeforeWriteAsync { get; set; }
    internal Func<CancellationToken, Task>? BeforeMetadataAsync { get; set; }
    internal Func<string, Exception?>? ReadFailure { get; set; }
    internal int ReadCount => Volatile.Read(ref _readCount);
    internal int WriteCount => Volatile.Read(ref _writeCount);
    internal int ActiveOperations => Volatile.Read(ref _activeOperations);
    internal IReadOnlyCollection<string> Operations => _operations.ToArray();

    internal void Seed(
        string path,
        string content,
        DateTimeOffset? modified = null
    )
        => _files[Canonical(path)] = new StoredFile(
            Encoding.UTF8.GetBytes(content),
            modified ?? DateTimeOffset.UtcNow
        );

    internal bool TryReadSeeded(string path, out string content)
    {
        if (_files.TryGetValue(Canonical(path), out var file))
        {
            content = Encoding.UTF8.GetString(file.Content);
            return true;
        }

        content = "";
        return false;
    }

    string ISaveStore.ReadFile(string path)
        => ReadFile(path);

    async Task<string?> ISaveStore.ReadFileAsync(string path)
        => await ReadFileAsync(path, CancellationToken.None);

    Task<string> ICancellableSaveStore.ReadFileAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileAsync(path, cancellationToken);

    void ISaveStore.WriteFile(string path, string content)
        => WriteFile(path, Encoding.UTF8.GetBytes(content));

    void ISaveStore.WriteFile(string path, byte[] bytes)
        => WriteFile(path, bytes);

    Task ISaveStore.WriteFileAsync(string path, string content)
        => WriteFileAsync(path, content, CancellationToken.None);

    Task ISaveStore.WriteFileAsync(string path, byte[] bytes)
    {
        WriteFile(path, bytes);
        return Task.CompletedTask;
    }

    Task ICancellableSaveStore.WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken
    )
        => WriteFileAsync(path, content, cancellationToken);

    bool ISaveStore.FileExists(string path)
        => _files.ContainsKey(Canonical(path));

    void ISaveStore.DeleteFile(string path)
    {
        _files.TryRemove(Canonical(path), out _);
        Record($"delete:{Canonical(path)}");
    }

    void ISaveStore.RenameFile(string sourcePath, string destinationPath)
    {
        var source = Canonical(sourcePath);
        if (!_files.TryRemove(source, out var file))
            throw new FileNotFoundException($"Missing in-memory file: {source}");

        _files[Canonical(destinationPath)] = file;
        Record($"rename:{source}->{Canonical(destinationPath)}");
    }

    DateTimeOffset ISaveStore.GetLastModifiedTime(string path)
        => GetFile(path).Modified;

    int ISaveStore.GetFileSize(string path)
        => GetFile(path).Content.Length;

    void ISaveStore.SetLastModifiedTime(string path, DateTimeOffset time)
    {
        var canonical = Canonical(path);
        _files.AddOrUpdate(
            canonical,
            _ => throw new FileNotFoundException(
                $"Missing in-memory file: {canonical}"
            ),
            (_, file) => file with { Modified = time }
        );
    }

    string ISaveStore.GetFullPath(string filename)
        => $"memory://{_name}/{Canonical(filename)}";

    bool ISaveStore.DirectoryExists(string path)
    {
        var prefix = DirectoryPrefix(path);
        return _files.Keys.Any(key => key.StartsWith(
            prefix,
            StringComparison.OrdinalIgnoreCase
        ));
    }

    string[] ISaveStore.GetFilesInDirectory(string directoryPath)
    {
        var prefix = DirectoryPrefix(directoryPath);
        return _files.Keys
            .Where(path => path.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase
            ))
            .Select(path => path[prefix.Length..])
            .Where(remainder => !remainder.Contains('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    string[] ISaveStore.GetDirectoriesInDirectory(string directoryPath)
    {
        var prefix = DirectoryPrefix(directoryPath);
        return _files.Keys
            .Where(path => path.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase
            ))
            .Select(path => path[prefix.Length..])
            .Where(remainder => remainder.Contains('/'))
            .Select(remainder => remainder[..remainder.IndexOf('/')])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    void ISaveStore.CreateDirectory(string directoryPath)
        => Record($"mkdir:{Canonical(directoryPath)}");

    void ISaveStore.DeleteDirectory(string directoryPath)
    {
        var prefix = DirectoryPrefix(directoryPath);
        foreach (var path in _files.Keys.Where(path => path.StartsWith(
            prefix,
            StringComparison.OrdinalIgnoreCase
        )))
        {
            _files.TryRemove(path, out _);
        }
        Record($"rmdir:{Canonical(directoryPath)}");
    }

    void ISaveStore.DeleteTemporaryFiles(string directoryPath)
    {
        var prefix = DirectoryPrefix(directoryPath);
        foreach (var path in _files.Keys.Where(path =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)))
        {
            _files.TryRemove(path, out _);
        }
    }

    void ICloudSaveStore.BeginSaveBatch()
        => Record("batch:begin");

    void ICloudSaveStore.EndSaveBatch()
        => Record("batch:end");

    bool ICloudSaveStore.HasCloudFiles()
        => !_files.IsEmpty;

    void ICloudSaveStore.ForgetFile(string path)
        => _files.TryRemove(Canonical(path), out _);

    bool ICloudSaveStore.IsFilePersisted(string path)
        => _files.ContainsKey(Canonical(path));

    public bool HasUserEnabledCloudSync()
        => true;

    void ICancellableCloudMetadataStore.PrepareFileMetadata(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var preparation = BeforeMetadataAsync;
        if (preparation is not null)
        {
            preparation(cancellationToken)
                .WaitAsync(cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        cancellationToken.ThrowIfCancellationRequested();
        Record("metadata:prepared");
    }

    private string ReadFile(string path)
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _readCount);
        Record($"read:{canonical}");
        var failure = ReadFailure?.Invoke(canonical);
        if (failure is not null)
            throw failure;
        return Encoding.UTF8.GetString(GetFile(canonical).Content);
    }

    private async Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _activeOperations);
        try
        {
            Interlocked.Increment(ref _readCount);
            Record($"read-async:{canonical}");
            var beforeRead = BeforeReadAsync;
            if (beforeRead is not null)
                await beforeRead(canonical, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var failure = ReadFailure?.Invoke(canonical);
            if (failure is not null)
                throw failure;
            return Encoding.UTF8.GetString(GetFile(canonical).Content);
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    private void WriteFile(string path, byte[] content)
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _writeCount);
        _files[canonical] = new StoredFile(
            content.ToArray(),
            DateTimeOffset.UtcNow
        );
        Record($"write:{canonical}");
    }

    private async Task WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken
    )
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _activeOperations);
        try
        {
            var beforeWrite = BeforeWriteAsync;
            if (beforeWrite is not null)
                await beforeWrite(canonical, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            WriteFile(canonical, Encoding.UTF8.GetBytes(content));
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    private StoredFile GetFile(string path)
    {
        var canonical = Canonical(path);
        return _files.TryGetValue(canonical, out var file)
            ? file
            : throw new FileNotFoundException(
                $"Missing in-memory file: {canonical}"
            );
    }

    private void Record(string operation)
        => _operations.Enqueue(operation);

    private static string Canonical(string path)
        => path.Replace('\\', '/').Trim('/');

    private static string DirectoryPrefix(string path)
    {
        var canonical = Canonical(path);
        return canonical.Length == 0 ? "" : $"{canonical}/";
    }

    private sealed record StoredFile(
        byte[] Content,
        DateTimeOffset Modified
    );
}
