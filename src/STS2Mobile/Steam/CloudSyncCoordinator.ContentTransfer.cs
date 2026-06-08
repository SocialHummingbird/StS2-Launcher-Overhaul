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
        int timeoutMs
    )
    {
        var cloudTime = cloud.GetLastModifiedTime(path);
        var writeTask = local.WriteFileAsync(path, content);
        await WaitForCloudOperationAsync(
            $"WriteLocalFile {path}",
            timeoutMs,
            writeTask
        ).ConfigureAwait(false);
        local.SetLastModifiedTime(path, cloudTime);
        PatchHelper.Log($"[Cloud] Local write path: {path} -> {local.GetFullPath(path)}");
    }

    private static async Task<string> ReadCloudContentAsync(
        ICloudSaveStore cloud,
        string path,
        string operation,
        int timeoutMs
    )
    {
        var task = cloud.ReadFileAsync(path);
        return await WaitForCloudOperationAsync(
            $"{operation} {path}",
            timeoutMs,
            task
        ).ConfigureAwait(false);
    }
}
