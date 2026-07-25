#nullable enable

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

internal static class CancellableSaveStore
{
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
}
