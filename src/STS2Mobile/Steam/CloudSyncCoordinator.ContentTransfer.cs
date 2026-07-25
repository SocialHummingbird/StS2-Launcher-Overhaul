using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task WriteLocalContentFromCloudAsync(
        ISaveStore local,
        ICloudSaveStore cloud,
        string path,
        string content,
        int timeoutMs,
        CancellationToken cancellationToken = default
    )
    {
        var cloudTime = cloud.GetLastModifiedTime(path);
        await WaitForCloudOperationAsync(
            $"WriteLocalFile {path}",
            timeoutMs,
            token => CancellableSaveStore.WriteFileAsync(
                local,
                path,
                content,
                token
            ),
            cancellationToken
        ).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        local.SetLastModifiedTime(path, cloudTime);
        PatchHelper.Log($"[Cloud] Local write path: {path} -> {local.GetFullPath(path)}");
    }

    private static async Task<string> ReadCloudContentAsync(
        ICloudSaveStore cloud,
        string path,
        string operation,
        int timeoutMs,
        CancellationToken cancellationToken = default
    )
    {
        return await WaitForCloudOperationAsync(
            $"{operation} {path}",
            timeoutMs,
            token => CancellableSaveStore.ReadFileAsync(
                cloud,
                path,
                token
            ),
            cancellationToken
        ).ConfigureAwait(false);
    }
}
