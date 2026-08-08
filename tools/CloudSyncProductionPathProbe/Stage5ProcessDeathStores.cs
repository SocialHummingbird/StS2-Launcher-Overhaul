#nullable enable

using System.Globalization;
using System.Text;
using System.Text.Json;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Steam;

namespace CloudSyncProductionPathProbe;

/// <summary>
/// Test-only observation wrapper around the production Android filesystem
/// store. The wrapped store still performs every local read, atomic write,
/// rename, and delete; this class only exposes the same before/after mutation
/// edges used by the existing in-memory probe.
/// </summary>
internal sealed class ObservedAndroidLocalSaveStore :
    ISaveStore,
    ICancellableSaveStore,
    IRawSaveStore,
    IRecoverySaveStore
{
    private readonly AndroidLocalSaveStore _inner;
    private readonly Action<StoreMutation>? _observe;

    internal ObservedAndroidLocalSaveStore(
        string basePath,
        Action<StoreMutation>? observe = null
    )
    {
        _inner = new AndroidLocalSaveStore(basePath);
        _observe = observe;
    }

    private ISaveStore Store => _inner;
    private ICancellableSaveStore Cancellable => _inner;
    private IRawSaveStore Raw => _inner;
    private IRecoverySaveStore Recovery => _inner;

    string ISaveStore.ReadFile(string path)
        => Store.ReadFile(path) ?? throw new FileNotFoundException(
            $"Android fixture file disappeared while it was read: {path}"
        );

    Task<string?> ISaveStore.ReadFileAsync(string path)
        => Store.ReadFileAsync(path);

    Task<string> ICancellableSaveStore.ReadFileAsync(
        string path,
        CancellationToken cancellationToken
    )
        => Cancellable.ReadFileAsync(path, cancellationToken);

    Task<byte[]> IRawSaveStore.ReadFileBytesAsync(
        string path,
        CancellationToken cancellationToken
    )
        => Raw.ReadFileBytesAsync(path, cancellationToken);

    void ISaveStore.WriteFile(string path, string content)
        => ObserveWrite(path, () => Store.WriteFile(path, content));

    void ISaveStore.WriteFile(string path, byte[] bytes)
        => ObserveWrite(path, () => Store.WriteFile(path, bytes));

    Task ISaveStore.WriteFileAsync(string path, string content)
        => ObserveWriteAsync(path, () => Store.WriteFileAsync(path, content));

    Task ISaveStore.WriteFileAsync(string path, byte[] bytes)
        => ObserveWriteAsync(path, () => Store.WriteFileAsync(path, bytes));

    Task ICancellableSaveStore.WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken
    )
        => ObserveWriteAsync(
            path,
            () => Cancellable.WriteFileAsync(path, content, cancellationToken)
        );

    Task IRawSaveStore.WriteFileBytesAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
        => ObserveWriteAsync(
            path,
            () => Raw.WriteFileBytesAsync(path, content, cancellationToken)
        );

    Task IRecoverySaveStore.WriteRecoveryFileBytesAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
        => ObserveWriteAsync(
            path,
            () => Recovery.WriteRecoveryFileBytesAsync(
                path,
                content,
                cancellationToken
            )
        );

    Task IRecoverySaveStore.DeleteRecoveryFileAsync(
        string path,
        CancellationToken cancellationToken
    )
        => ObserveDeleteAsync(
            path,
            () => Recovery.DeleteRecoveryFileAsync(path, cancellationToken)
        );

    bool ISaveStore.FileExists(string path) => Store.FileExists(path);

    void ISaveStore.DeleteFile(string path)
        => ObserveDelete(path, () => Store.DeleteFile(path));

    void ISaveStore.RenameFile(string sourcePath, string destinationPath)
    {
        Observe(
            StoreMutationKind.Rename,
            StoreMutationEdge.Before,
            sourcePath,
            destinationPath
        );
        Store.RenameFile(sourcePath, destinationPath);
        Observe(
            StoreMutationKind.Rename,
            StoreMutationEdge.After,
            sourcePath,
            destinationPath
        );
    }

    DateTimeOffset ISaveStore.GetLastModifiedTime(string path)
        => Store.GetLastModifiedTime(path);

    int ISaveStore.GetFileSize(string path) => Store.GetFileSize(path);

    void ISaveStore.SetLastModifiedTime(string path, DateTimeOffset time)
        => Store.SetLastModifiedTime(path, time);

    string ISaveStore.GetFullPath(string filename) => Store.GetFullPath(filename);

    bool ISaveStore.DirectoryExists(string path) => Store.DirectoryExists(path);

    string[] ISaveStore.GetFilesInDirectory(string directoryPath)
        => Store.GetFilesInDirectory(directoryPath);

    string[] ISaveStore.GetDirectoriesInDirectory(string directoryPath)
        => Store.GetDirectoriesInDirectory(directoryPath);

    void ISaveStore.CreateDirectory(string directoryPath)
        => Store.CreateDirectory(directoryPath);

    void ISaveStore.DeleteDirectory(string directoryPath)
    {
        Observe(
            StoreMutationKind.DeleteDirectory,
            StoreMutationEdge.Before,
            directoryPath
        );
        Store.DeleteDirectory(directoryPath);
        Observe(
            StoreMutationKind.DeleteDirectory,
            StoreMutationEdge.After,
            directoryPath
        );
    }

    void ISaveStore.DeleteTemporaryFiles(string directoryPath)
    {
        Observe(
            StoreMutationKind.DeleteTemporaryFiles,
            StoreMutationEdge.Before,
            directoryPath
        );
        Store.DeleteTemporaryFiles(directoryPath);
        Observe(
            StoreMutationKind.DeleteTemporaryFiles,
            StoreMutationEdge.After,
            directoryPath
        );
    }

    private void ObserveWrite(string path, Action write)
    {
        Observe(StoreMutationKind.Write, StoreMutationEdge.Before, path);
        write();
        Observe(StoreMutationKind.Write, StoreMutationEdge.After, path);
    }

    private async Task ObserveWriteAsync(string path, Func<Task> write)
    {
        Observe(StoreMutationKind.Write, StoreMutationEdge.Before, path);
        await write().ConfigureAwait(false);
        Observe(StoreMutationKind.Write, StoreMutationEdge.After, path);
    }

    private void ObserveDelete(string path, Action delete)
    {
        Observe(StoreMutationKind.Delete, StoreMutationEdge.Before, path);
        delete();
        Observe(StoreMutationKind.Delete, StoreMutationEdge.After, path);
    }

    private async Task ObserveDeleteAsync(string path, Func<Task> delete)
    {
        Observe(StoreMutationKind.Delete, StoreMutationEdge.Before, path);
        await delete().ConfigureAwait(false);
        Observe(StoreMutationKind.Delete, StoreMutationEdge.After, path);
    }

    private void Observe(
        StoreMutationKind kind,
        StoreMutationEdge edge,
        string path,
        string destinationPath = ""
    )
        => _observe?.Invoke(new StoreMutation(
            "local",
            kind,
            edge,
            Canonical(path),
            Canonical(destinationPath)
        ));

    private static string Canonical(string path)
        => string.IsNullOrWhiteSpace(path) ? "" : CloudSavePath.Relative(path);
}

internal static class PersistedFakeSteamStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        WriteIndented = false,
    };

    internal static InMemoryCloudSaveStore Load(string path, string name)
    {
        if (!File.Exists(path))
            return new InMemoryCloudSaveStore(name);

        var json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<CloudStateDocument>(
            json,
            JsonOptions
        ) ?? throw new InvalidDataException("Persisted fake Steam state is empty.");
        var files = document.Files.Select(file => (
            file.Path,
            Convert.FromBase64String(file.ContentBase64),
            DateTimeOffset.ParseExact(
                file.ModifiedUtc,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind
            )
        ));
        return InMemoryCloudSaveStore.FromState(
            name,
            new InMemoryCloudSaveStoreState(document.SteamId64, files)
        );
    }

    internal static void Save(string path, InMemoryCloudSaveStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        var state = store.ExportState();
        var document = new CloudStateDocument
        {
            SteamId64 = state.SteamId64,
            Files = state.CopyFiles()
                .OrderBy(file => file.Path, StringComparer.OrdinalIgnoreCase)
                .Select(file => new CloudFileDocument
                {
                    Path = CloudSavePath.Relative(file.Path),
                    ContentBase64 = Convert.ToBase64String(file.Content),
                    ModifiedUtc = file.Modified.ToUniversalTime().ToString(
                        "O",
                        CultureInfo.InvariantCulture
                    ),
                })
                .ToList(),
        };
        AtomicWriteText(path, JsonSerializer.Serialize(document, JsonOptions));
    }

    internal static void AtomicWriteText(string path, string content)
    {
        var fullPath = Path.GetFullPath(path);
        var parent = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("Persisted state path has no parent.");
        Directory.CreateDirectory(parent);
        var staging = $"{fullPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            using (var stream = new FileStream(
                staging,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.WriteThrough
            ))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(flushToDisk: true);
            }
            File.Move(staging, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(staging))
                File.Delete(staging);
        }
    }

    private sealed class CloudStateDocument
    {
        public ulong SteamId64 { get; set; }
        public List<CloudFileDocument> Files { get; set; } = new();
    }

    private sealed class CloudFileDocument
    {
        public string Path { get; set; } = "";
        public string ContentBase64 { get; set; } = "";
        public string ModifiedUtc { get; set; } = "";
    }
}
