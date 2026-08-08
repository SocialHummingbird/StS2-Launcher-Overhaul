#nullable enable

using System.Collections.Concurrent;
using System.Text;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

internal enum StoreMutationKind
{
    Write,
    Delete,
    Rename,
    DeleteDirectory,
    DeleteTemporaryFiles,
}

internal enum StoreMutationEdge
{
    Before,
    After,
}

internal readonly record struct StoreMutation(
    string Store,
    StoreMutationKind Kind,
    StoreMutationEdge Edge,
    string Path,
    string DestinationPath = ""
);

internal sealed class InMemoryCloudSaveStoreState
{
    private readonly IReadOnlyList<FileState> _files;

    internal InMemoryCloudSaveStoreState(
        ulong steamId64,
        IEnumerable<(string Path, byte[] Content, DateTimeOffset Modified)> files
    )
    {
        SteamId64 = steamId64;
        _files = files
            .Select(file => new FileState(
                file.Path,
                file.Content.ToArray(),
                file.Modified
            ))
            .ToArray();
    }

    internal ulong SteamId64 { get; }

    internal IEnumerable<(string Path, byte[] Content, DateTimeOffset Modified)>
        CopyFiles()
        => _files.Select(file => (
            file.Path,
            file.Content.ToArray(),
            file.Modified
        ));

    private sealed record FileState(
        string Path,
        byte[] Content,
        DateTimeOffset Modified
    );
}

internal sealed class InMemoryCloudSaveStore :
    ISaveStore,
    ICloudSaveStore,
    ICancellableSaveStore,
    IRawSaveStore,
    IRecoverySaveStore,
    ICancellableCloudMetadataStore,
    ITransferSaveStore
{
    internal const ulong DefaultSteamId64 = 76561198000000001UL;

    private readonly ConcurrentDictionary<string, StoredFile> _files =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _pathWrites =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> _operations = new();
    private readonly Action<string>? _sharedTrace;
    private readonly string _name;
    private int _activeOperations;
    private int _deleteCount;
    private int _readCount;
    private int _rawWriteCount;
    private int _recoveryDeleteCount;
    private int _recoveryWriteCount;
    private int _writeCount;

    internal InMemoryCloudSaveStore(
        string name,
        ulong steamId64 = DefaultSteamId64,
        Action<string>? sharedTrace = null
    )
    {
        _name = name;
        SteamId64 = steamId64;
        _sharedTrace = sharedTrace;
    }

    internal ulong SteamId64 { get; set; }
    internal Exception? AuthenticationFailure { get; set; }
    internal Func<CancellationToken, Task>? BeforeAuthenticateAsync { get; set; }
    internal Func<string, CancellationToken, Task>? BeforeReadAsync { get; set; }
    internal Func<string, CancellationToken, Task>? BeforeVerificationReadAsync { get; set; }
    internal Func<string, CancellationToken, Task>? BeforeVerificationExistsAsync { get; set; }
    internal Func<string, CancellationToken, Task>? BeforeWriteAsync { get; set; }
    internal Func<string, CancellationToken, Task>? BeforeDeleteAsync { get; set; }
    internal Action<string>? BeforeSynchronousWrite { get; set; }
    internal Func<CancellationToken, Task>? BeforeMetadataAsync { get; set; }
    internal Func<string, Exception?>? ReadFailure { get; set; }
    internal Func<string, Exception?>? WriteFailure { get; set; }
    internal Func<string, Exception?>? DeleteFailure { get; set; }
    internal Func<string, Exception?>? ListFilesFailure { get; set; }
    internal Func<string, string[], string[]>? FileListingTransform { get; set; }
    internal Func<string, string, string>? VerificationReadTransform { get; set; }
    internal Func<string, byte[], byte[]>? VerificationRawReadTransform { get; set; }
    internal Func<string, bool, bool>? VerificationExistsTransform { get; set; }
    internal Action<StoreMutation>? MutationObserved { get; set; }

    internal int ReadCount => Volatile.Read(ref _readCount);
    internal int RawWriteCount => Volatile.Read(ref _rawWriteCount);
    internal int RecoveryDeleteCount => Volatile.Read(ref _recoveryDeleteCount);
    internal int RecoveryWriteCount => Volatile.Read(ref _recoveryWriteCount);
    internal int WriteCount => Volatile.Read(ref _writeCount);
    internal int DeleteCount => Volatile.Read(ref _deleteCount);
    internal int ActiveOperations => Volatile.Read(ref _activeOperations);
    internal IReadOnlyCollection<string> Operations => _operations.ToArray();
    internal IReadOnlyCollection<string> Paths => _files.Keys
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    internal InMemoryCloudSaveStoreState ExportState()
    {
        var files = _files
            .ToArray()
            .Select(entry => (
                entry.Key,
                entry.Value.Content.ToArray(),
                entry.Value.Modified
            ))
            .ToArray();
        return new InMemoryCloudSaveStoreState(SteamId64, files);
    }

    internal InMemoryCloudSaveStore Fork(
        string name,
        Action<string>? sharedTrace = null
    )
        => FromState(name, ExportState(), sharedTrace);

    internal static InMemoryCloudSaveStore FromState(
        string name,
        InMemoryCloudSaveStoreState state,
        Action<string>? sharedTrace = null
    )
    {
        ArgumentNullException.ThrowIfNull(state);
        var store = new InMemoryCloudSaveStore(
            name,
            state.SteamId64,
            sharedTrace
        );
        foreach (var file in state.CopyFiles())
        {
            store._files[file.Path] = new StoredFile(
                file.Content,
                file.Modified
            );
        }

        return store;
    }

    internal void Seed(
        string path,
        string content,
        DateTimeOffset? modified = null
    )
        => _files[Canonical(path)] = new StoredFile(
            Encoding.UTF8.GetBytes(content),
            modified ?? DateTimeOffset.UtcNow
        );

    internal void SeedBytes(
        string path,
        byte[] content,
        DateTimeOffset? modified = null
    )
        => _files[Canonical(path)] = new StoredFile(
            content.ToArray(),
            modified ?? DateTimeOffset.UtcNow
        );

    internal bool Contains(string path)
        => _files.ContainsKey(Canonical(path));

    internal int WriteCountFor(string path)
        => _pathWrites.TryGetValue(Canonical(path), out var count) ? count : 0;

    internal bool TryReadSeeded(string path, out string content)
    {
        if (_files.TryGetValue(Canonical(path), out var file))
        {
            content = DecodeText(file.Content);
            return true;
        }

        content = "";
        return false;
    }

    internal bool TryReadSeededBytes(string path, out byte[] content)
    {
        if (_files.TryGetValue(Canonical(path), out var file))
        {
            content = file.Content.ToArray();
            return true;
        }

        content = Array.Empty<byte>();
        return false;
    }

    string ISaveStore.ReadFile(string path)
        => ReadFile(path);

    async Task<string?> ISaveStore.ReadFileAsync(string path)
        => await ReadFileAsync(path, CancellationToken.None).ConfigureAwait(false);

    Task<string> ICancellableSaveStore.ReadFileAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileAsync(path, cancellationToken);

    Task<byte[]> IRawSaveStore.ReadFileBytesAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileBytesAsync(path, cancellationToken);

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

    async Task IRawSaveStore.WriteFileBytesAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
    {
        await WriteFileBytesAsync(
            path,
            content,
            cancellationToken
        ).ConfigureAwait(false);
        Interlocked.Increment(ref _rawWriteCount);
    }

    async Task IRecoverySaveStore.WriteRecoveryFileBytesAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
    {
        await WriteFileBytesAsync(
            path,
            content,
            cancellationToken
        ).ConfigureAwait(false);
        Interlocked.Increment(ref _recoveryWriteCount);
        Record($"recovery-write:{Canonical(path)}");
    }

    async Task IRecoverySaveStore.DeleteRecoveryFileAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        await ((ITransferSaveStore)this).DeleteFileAsync(
            path,
            cancellationToken
        ).ConfigureAwait(false);
        Interlocked.Increment(ref _recoveryDeleteCount);
        Record($"recovery-delete:{Canonical(path)}");
    }

    bool ISaveStore.FileExists(string path)
        => Contains(path);

    void ISaveStore.DeleteFile(string path)
    {
        var canonical = Canonical(path);
        ObserveMutation(
            StoreMutationKind.Delete,
            StoreMutationEdge.Before,
            canonical
        );
        ThrowConfigured(DeleteFailure?.Invoke(canonical));
        _files.TryRemove(canonical, out _);
        Interlocked.Increment(ref _deleteCount);
        Record($"delete:{canonical}");
        ObserveMutation(
            StoreMutationKind.Delete,
            StoreMutationEdge.After,
            canonical
        );
    }

    void ISaveStore.RenameFile(string sourcePath, string destinationPath)
    {
        var source = Canonical(sourcePath);
        var destination = Canonical(destinationPath);
        ObserveMutation(
            StoreMutationKind.Rename,
            StoreMutationEdge.Before,
            source,
            destination
        );
        if (!_files.TryRemove(source, out var file))
            throw new FileNotFoundException($"Missing in-memory file: {source}");

        _files[destination] = file;
        Record($"rename:{source}->{destination}");
        ObserveMutation(
            StoreMutationKind.Rename,
            StoreMutationEdge.After,
            source,
            destination
        );
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
        Record($"directory-exists:{Canonical(path)}");
        return _files.Keys.Any(key => key.StartsWith(
            prefix,
            StringComparison.OrdinalIgnoreCase
        ));
    }

    string[] ISaveStore.GetFilesInDirectory(string directoryPath)
    {
        var prefix = DirectoryPrefix(directoryPath);
        var canonical = Canonical(directoryPath);
        Record($"list-files:{canonical}");
        ThrowConfigured(ListFilesFailure?.Invoke(canonical));
        var files = _files.Keys
            .Where(path => path.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase
            ))
            .Select(path => path[prefix.Length..])
            .Where(remainder => !remainder.Contains('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return FileListingTransform?.Invoke(canonical, files) ?? files;
    }

    string[] ISaveStore.GetDirectoriesInDirectory(string directoryPath)
    {
        var prefix = DirectoryPrefix(directoryPath);
        Record($"list-directories:{Canonical(directoryPath)}");
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
        var canonical = Canonical(directoryPath);
        ObserveMutation(
            StoreMutationKind.DeleteDirectory,
            StoreMutationEdge.Before,
            canonical
        );
        var prefix = DirectoryPrefix(directoryPath);
        foreach (var path in _files.Keys.Where(path => path.StartsWith(
            prefix,
            StringComparison.OrdinalIgnoreCase
        )))
        {
            _files.TryRemove(path, out _);
        }
        Record($"rmdir:{canonical}");
        ObserveMutation(
            StoreMutationKind.DeleteDirectory,
            StoreMutationEdge.After,
            canonical
        );
    }

    void ISaveStore.DeleteTemporaryFiles(string directoryPath)
    {
        var canonical = Canonical(directoryPath);
        ObserveMutation(
            StoreMutationKind.DeleteTemporaryFiles,
            StoreMutationEdge.Before,
            canonical
        );
        var prefix = DirectoryPrefix(directoryPath);
        foreach (var path in _files.Keys.Where(path =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)))
        {
            _files.TryRemove(path, out _);
        }
        ObserveMutation(
            StoreMutationKind.DeleteTemporaryFiles,
            StoreMutationEdge.After,
            canonical
        );
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
        => Contains(path);

    public bool HasUserEnabledCloudSync()
        => true;

    void ICancellableCloudMetadataStore.PrepareFileMetadata(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        Record("metadata:prepare");
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

    async Task<ulong> ITransferSaveStore.GetAuthenticatedSteamId64Async(
        CancellationToken cancellationToken
    )
    {
        Interlocked.Increment(ref _activeOperations);
        try
        {
            Record("authenticate");
            if (BeforeAuthenticateAsync is not null)
                await BeforeAuthenticateAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ThrowConfigured(AuthenticationFailure);
            if (SteamId64 == 0)
                throw new InvalidOperationException("Fake Steam authentication returned SteamID64 0.");
            return SteamId64;
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    async Task ITransferSaveStore.DeleteFileAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _activeOperations);
        try
        {
            Record($"delete-async:start:{canonical}");
            if (BeforeDeleteAsync is not null)
                await BeforeDeleteAsync(canonical, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ObserveMutation(
                StoreMutationKind.Delete,
                StoreMutationEdge.Before,
                canonical
            );
            ThrowConfigured(DeleteFailure?.Invoke(canonical));
            _files.TryRemove(canonical, out _);
            Interlocked.Increment(ref _deleteCount);
            Record($"delete-async:complete:{canonical}");
            ObserveMutation(
                StoreMutationKind.Delete,
                StoreMutationEdge.After,
                canonical
            );
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    Task<string> ITransferSaveStore.ReadFileForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileAsync(path, cancellationToken, verification: true);

    Task<byte[]> ITransferSaveStore.ReadFileBytesForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileBytesAsync(path, cancellationToken, verification: true);

    async Task<bool> ITransferSaveStore.FileExistsForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _activeOperations);
        try
        {
            Record($"verify-exists:{canonical}");
            if (BeforeVerificationExistsAsync is not null)
            {
                await BeforeVerificationExistsAsync(canonical, cancellationToken)
                    .ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var exists = _files.ContainsKey(canonical);
            return VerificationExistsTransform?.Invoke(canonical, exists) ?? exists;
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    private string ReadFile(string path)
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _readCount);
        Record($"read:{canonical}");
        ThrowConfigured(ReadFailure?.Invoke(canonical));
        return DecodeText(GetFile(canonical).Content);
    }

    private Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ReadFileAsync(path, cancellationToken, verification: false);

    private async Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken,
        bool verification
    )
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _activeOperations);
        try
        {
            Interlocked.Increment(ref _readCount);
            Record($"{(verification ? "verify-read" : "read-async")}:{canonical}");
            var beforeRead = verification
                ? BeforeVerificationReadAsync ?? BeforeReadAsync
                : BeforeReadAsync;
            if (beforeRead is not null)
                await beforeRead(canonical, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ThrowConfigured(ReadFailure?.Invoke(canonical));
            var content = DecodeText(GetFile(canonical).Content);
            return verification && VerificationReadTransform is not null
                ? VerificationReadTransform(canonical, content)
                : content;
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    private async Task<byte[]> ReadFileBytesAsync(
        string path,
        CancellationToken cancellationToken,
        bool verification = false
    )
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _activeOperations);
        try
        {
            Interlocked.Increment(ref _readCount);
            Record($"{(verification ? "verify-raw-read" : "raw-read")}:{canonical}");
            var beforeRead = verification
                ? BeforeVerificationReadAsync ?? BeforeReadAsync
                : BeforeReadAsync;
            if (beforeRead is not null)
                await beforeRead(canonical, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ThrowConfigured(ReadFailure?.Invoke(canonical));
            var content = GetFile(canonical).Content.ToArray();
            return verification && VerificationRawReadTransform is not null
                ? VerificationRawReadTransform(canonical, content).ToArray()
                : content;
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    private void WriteFile(string path, byte[] content)
    {
        var canonical = Canonical(path);
        BeforeSynchronousWrite?.Invoke(canonical);
        ObserveMutation(
            StoreMutationKind.Write,
            StoreMutationEdge.Before,
            canonical
        );
        ThrowConfigured(WriteFailure?.Invoke(canonical));
        Interlocked.Increment(ref _writeCount);
        _pathWrites.AddOrUpdate(canonical, 1, (_, count) => count + 1);
        _files[canonical] = new StoredFile(
            content.ToArray(),
            DateTimeOffset.UtcNow
        );
        Record($"write:{canonical}");
        ObserveMutation(
            StoreMutationKind.Write,
            StoreMutationEdge.After,
            canonical
        );
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
            Record($"write-async:start:{canonical}");
            if (BeforeWriteAsync is not null)
                await BeforeWriteAsync(canonical, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            WriteFile(canonical, Encoding.UTF8.GetBytes(content));
            Record($"write-async:complete:{canonical}");
        }
        finally
        {
            Interlocked.Decrement(ref _activeOperations);
        }
    }

    private async Task WriteFileBytesAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
    {
        var canonical = Canonical(path);
        Interlocked.Increment(ref _activeOperations);
        try
        {
            Record($"raw-write:start:{canonical}");
            if (BeforeWriteAsync is not null)
                await BeforeWriteAsync(canonical, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            WriteFile(canonical, content);
            Record($"raw-write:complete:{canonical}");
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
    {
        _operations.Enqueue(operation);
        _sharedTrace?.Invoke($"{_name}:{operation}");
    }

    private void ObserveMutation(
        StoreMutationKind kind,
        StoreMutationEdge edge,
        string path,
        string destinationPath = ""
    )
        => MutationObserved?.Invoke(new StoreMutation(
            _name,
            kind,
            edge,
            path,
            destinationPath
        ));

    private static void ThrowConfigured(Exception? failure)
    {
        if (failure is not null)
            throw failure;
    }

    private static string DecodeText(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: false
        );
        return reader.ReadToEnd();
    }

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
