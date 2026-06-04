using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly partial struct AutoSyncContext
    {
        private async Task PullCloudOnlyFileAsync()
        {
            string cloud = await ReadCloudContentAsync(PullCloudFileOperation);
            await PullCloudOnlyContentAsync(cloud, PullDownloaded);
        }

        private void PushLocalOnlyFile(string localContent)
        {
            PushLocalContent(localContent, cloudContent: null, PushUploaded);
        }

        private async Task PullCloudOnlyContentAsync(
            string content,
            Func<string, string> message
        )
        {
            Log(message);
            await WriteLocalContentFromCloudAsync(content);
        }

        internal async Task PullCloudOverLocalAsync(
            string content,
            Func<string, string> message
        )
        {
            Log(message);
            BackUpLocalProgress();
            await WriteLocalContentFromCloudAsync(content);
        }

        internal void PushLocalContent(
            string localContent,
            string? cloudContent,
            Func<string, string> message
        )
        {
            Log(message);
            if (cloudContent != null)
                BackUpCloudProgress(cloudContent);
            WriteCloudFile(localContent);
        }

        private void BackUpLocalProgress()
            => SaveBackups.LocalProgressFile(_local, Path);

        private void BackUpCloudProgress(string content)
            => SaveBackups.CloudProgressContent(Path, content);
    }
}
