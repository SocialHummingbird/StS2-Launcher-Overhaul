using System;
using System.Threading.Tasks;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private readonly partial struct AutoSyncContext
    {
        private enum LocalProgressBackup
        {
            None,
            BeforeOverwrite,
        }

        private async Task PullCloudOnlyFileAsync()
        {
            string cloud = await ReadCloudContentAsync(PullCloudFileOperation);
            await PullCloudContentAsync(
                cloud,
                PullDownloaded,
                LocalProgressBackup.None
            );
        }

        private void PushLocalOnlyFile(string localContent)
        {
            PushLocalContent(localContent, cloudContent: null, PushUploaded);
        }

        internal async Task PullCloudOverLocalAsync(
            string content,
            Func<string, string> message
        )
        {
            await PullCloudContentAsync(
                content,
                message,
                LocalProgressBackup.BeforeOverwrite
            );
        }

        private async Task PullCloudContentAsync(
            string content,
            Func<string, string> message,
            LocalProgressBackup backup
        )
        {
            Log(message);
            if (backup == LocalProgressBackup.BeforeOverwrite)
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
