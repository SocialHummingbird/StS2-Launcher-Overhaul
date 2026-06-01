using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static async Task ApplyCloudWinsAsync(
        AutoSyncContext sync,
        string cloudContent,
        string message
    )
    {
        PatchHelper.Log(message);
        sync.BackUpLocalProgress();
        await sync.WriteCloudContentAsync(cloudContent);
    }

    private static void ApplyLocalWins(
        AutoSyncContext sync,
        string localContent,
        string cloudContent,
        string message
    )
    {
        PatchHelper.Log(message);
        sync.BackUpCloudProgress(cloudContent);
        sync.WriteCloudFile(localContent);
    }
}
