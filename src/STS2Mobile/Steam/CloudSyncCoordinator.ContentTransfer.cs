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
        await CloudOperationTimeout
            .For($"WriteLocalFile {path}", timeoutMs)
            .WaitAsync(writeTask)
            .ConfigureAwait(false);
        local.SetLastModifiedTime(path, cloudTime);
    }

    private static async Task<string> ReadCloudContentAsync(
        ICloudSaveStore cloud,
        string path,
        string operation,
        int timeoutMs
    )
    {
        var task = cloud.ReadFileAsync(path);
        return await CloudOperationTimeout
            .For($"{operation} {path}", timeoutMs)
            .WaitAsync(task)
            .ConfigureAwait(false);
    }
}
