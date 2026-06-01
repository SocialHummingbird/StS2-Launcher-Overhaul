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
        await sync.PullCloudContentAsync(cloudContent, message, backUpLocal: true);
    }

    private static void ApplyLocalWins(
        AutoSyncContext sync,
        string localContent,
        string cloudContent,
        string message
    )
    {
        sync.PushLocalContent(localContent, cloudContent, message);
    }
}
