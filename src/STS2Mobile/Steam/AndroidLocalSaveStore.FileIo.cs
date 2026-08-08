using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal sealed partial class AndroidLocalSaveStore
{
    private string ReadTextFile(string path) => File.ReadAllText(FullPath(path));

    private Task<string> ReadTextFileAsync(
        string path,
        CancellationToken cancellationToken
    )
        => File.ReadAllTextAsync(FullPath(path), cancellationToken);

    private Task<byte[]> ReadBytesFileAsync(
        string path,
        CancellationToken cancellationToken
    )
        => File.ReadAllBytesAsync(FullPath(path), cancellationToken);

    private void WriteTextFile(string path, string content)
    {
        WriteBytesFile(path, Encoding.UTF8.GetBytes(content));
    }

    private void WriteBytesFile(string path, byte[] bytes)
    {
        var fullPath = FullPath(path);
        EnsureParentDirectory(fullPath);

        CancellableAtomicFile.WriteAllBytesAsync(
            fullPath,
            bytes,
            overwrite: true,
            CancellationToken.None
        ).GetAwaiter().GetResult();
        PatchHelper.Log($"[Save] Android local save write: {path} -> {fullPath} ({bytes.Length} bytes)");
        CloudSyncCoordinator.MirrorLocalSaveWrite(path, bytes);
    }

    private Task WriteTextFileAsync(
        string path,
        string content,
        CancellationToken cancellationToken
    )
        => WriteBytesFileAsync(
            path,
            Encoding.UTF8.GetBytes(content),
            cancellationToken
        );

    private async Task WriteBytesFileAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken
    )
    {
        var fullPath = FullPath(path);
        EnsureParentDirectory(fullPath);
        await CancellableAtomicFile.WriteAllBytesAsync(
            fullPath,
            bytes,
            overwrite: true,
            cancellationToken
        ).ConfigureAwait(false);

        PatchHelper.Log($"[Save] Android local cancellable write: {path} -> {fullPath} ({bytes.Length} bytes)");
        CloudSyncCoordinator.MirrorLocalSaveWrite(
            path,
            bytes,
            cancellationToken
        );
    }

    private async Task WriteRecoveryBytesFileAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken
    )
    {
        var fullPath = FullPath(path);
        EnsureParentDirectory(fullPath);
        await CancellableAtomicFile.WriteAllBytesAsync(
            fullPath,
            bytes,
            overwrite: true,
            cancellationToken
        ).ConfigureAwait(false);
        PatchHelper.Log(
            $"[Recovery] Android local raw write without backup mirroring: {path} -> {fullPath} ({bytes.Length} bytes)"
        );
    }

    private Task DeleteRecoveryFileDirectAsync(
        string path,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = FullPath(path);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        cancellationToken.ThrowIfCancellationRequested();
        PatchHelper.Log(
            $"[Recovery] Android local delete without backup mirroring: {path} -> {fullPath}"
        );
        return Task.CompletedTask;
    }
}
