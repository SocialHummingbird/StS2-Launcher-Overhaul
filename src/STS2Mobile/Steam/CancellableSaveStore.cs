#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal interface ICancellableSaveStore
{
    Task<string> ReadFileAsync(
        string path,
        CancellationToken cancellationToken
    );

    Task WriteFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken
    );
}

internal interface ICancellableCloudMetadataStore
{
    void PrepareFileMetadata(CancellationToken cancellationToken);
}

internal interface IRawSaveStore
{
    Task<byte[]> ReadFileBytesAsync(
        string path,
        CancellationToken cancellationToken
    );

    Task WriteFileBytesAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    );
}

// Restore/Undo must not use the normal save-write path: Android mirrors normal
// writes into legacy/external backup locations that are recovery evidence.
internal interface IRecoverySaveStore
{
    Task WriteRecoveryFileBytesAsync(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    );

    Task DeleteRecoveryFileAsync(
        string path,
        CancellationToken cancellationToken
    );
}

internal interface ITransferSaveStore
{
    Task<ulong> GetAuthenticatedSteamId64Async(
        CancellationToken cancellationToken
    );

    Task DeleteFileAsync(
        string path,
        CancellationToken cancellationToken
    );

    Task<string> ReadFileForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    );

    Task<byte[]> ReadFileBytesForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    );

    Task<bool> FileExistsForVerificationAsync(
        string path,
        CancellationToken cancellationToken
    );
}

internal static class CancellableSaveStore
{
    internal static async Task<byte[]> ReadBytesAsync(
        ISaveStore store,
        string path,
        CancellationToken cancellationToken
    )
    {
        if (store is IRawSaveStore raw)
        {
            return await raw.ReadFileBytesAsync(
                path,
                cancellationToken
            ).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var content = await File.ReadAllBytesAsync(
            store.GetFullPath(path),
            cancellationToken
        ).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return content;
    }

    internal static async Task<string> ReadFileAsync(
        ISaveStore store,
        string path,
        CancellationToken cancellationToken
    )
    {
        if (store is ICancellableSaveStore cancellable)
        {
            return await cancellable.ReadFileAsync(
                path,
                cancellationToken
            ).ConfigureAwait(false);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var content = await store.ReadFileAsync(path).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return content!;
    }

    internal static async Task WriteFileAsync(
        ISaveStore store,
        string path,
        string content,
        CancellationToken cancellationToken
    )
    {
        if (store is ICancellableSaveStore cancellable)
        {
            await cancellable.WriteFileAsync(
                path,
                content,
                cancellationToken
            ).ConfigureAwait(false);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await store.WriteFileAsync(path, content).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal static async Task WriteBytesAsync(
        ISaveStore store,
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
    {
        if (store is IRawSaveStore raw)
        {
            await raw.WriteFileBytesAsync(
                path,
                content,
                cancellationToken
            ).ConfigureAwait(false);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await store.WriteFileAsync(path, content).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal static async Task WriteRecoveryBytesAsync(
        ISaveStore store,
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
    {
        if (store is IRecoverySaveStore recovery)
        {
            await recovery.WriteRecoveryFileBytesAsync(
                path,
                content,
                cancellationToken
            ).ConfigureAwait(false);
            return;
        }

        await WriteBytesAsync(
            store,
            path,
            content,
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal static async Task DeleteRecoveryFileAsync(
        ISaveStore store,
        string path,
        CancellationToken cancellationToken
    )
    {
        if (store is IRecoverySaveStore recovery)
        {
            await recovery.DeleteRecoveryFileAsync(
                path,
                cancellationToken
            ).ConfigureAwait(false);
            return;
        }

        await DeleteFileAsync(
            store,
            path,
            cancellationToken
        ).ConfigureAwait(false);
    }

    internal static async Task DeleteFileAsync(
        ISaveStore store,
        string path,
        CancellationToken cancellationToken
    )
    {
        if (store is ITransferSaveStore transfer)
        {
            await transfer.DeleteFileAsync(
                path,
                cancellationToken
            ).ConfigureAwait(false);
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        store.DeleteFile(path);
        cancellationToken.ThrowIfCancellationRequested();
    }

    internal static Task<string> ReadFileForVerificationAsync(
        ISaveStore store,
        string path,
        CancellationToken cancellationToken
    )
        => store is ITransferSaveStore transfer
            ? transfer.ReadFileForVerificationAsync(path, cancellationToken)
            : ReadFileAsync(store, path, cancellationToken);

    internal static Task<byte[]> ReadFileBytesForVerificationAsync(
        ISaveStore store,
        string path,
        CancellationToken cancellationToken
    )
        => store is ITransferSaveStore transfer
            ? transfer.ReadFileBytesForVerificationAsync(path, cancellationToken)
            : ReadBytesAsync(store, path, cancellationToken);

    internal static Task<bool> FileExistsForVerificationAsync(
        ISaveStore store,
        string path,
        CancellationToken cancellationToken
    )
    {
        if (store is ITransferSaveStore transfer)
        {
            return transfer.FileExistsForVerificationAsync(
                path,
                cancellationToken
            );
        }

        cancellationToken.ThrowIfCancellationRequested();
        var exists = store.FileExists(path);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(exists);
    }
}
