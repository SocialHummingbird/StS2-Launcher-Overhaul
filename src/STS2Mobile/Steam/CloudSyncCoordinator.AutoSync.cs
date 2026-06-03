using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int AutoSyncPerPathTimeoutMs = 30_000;
    private const string PullCloudFileOperation = "PullFile";
    private const string ReadCloudFileOperation = "ReadCloudFile";

    private readonly struct AutoSyncContext
    {
        private readonly ISaveStore _local;
        private readonly ICloudSaveStore _cloud;

        internal AutoSyncContext(ISaveStore local, ICloudSaveStore cloud, string path)
        {
            _local = local;
            _cloud = cloud;
            Path = path;
        }

        private string Path { get; }

        private bool CloudFileExists()
            => _cloud.FileExists(Path);

        private string? ReadLocalContent()
            => LocalFileExists() ? _local.ReadFile(Path) : null;

        internal Task<string> ReadCloudContentAsync(string operation)
            => CloudSyncCoordinator.ReadCloudContentAsync(
                _cloud,
                Path,
                operation,
                AutoSyncPerPathTimeoutMs
            );

        private Task WriteLocalContentFromCloudAsync(string content)
            => CloudSyncCoordinator.WriteLocalContentFromCloudAsync(
                _local,
                _cloud,
                Path,
                content,
                AutoSyncPerPathTimeoutMs
            );

        private void WriteCloudFile(string content)
            => _cloud.WriteFile(Path, content);

        internal SaveComparison.SaveWinner GetExplicitWinner(
            string localContent,
            string cloudContent
        )
            => SaveComparison.GetExplicitWinner(Path, localContent, cloudContent);

        internal void Log(Func<string, string> message)
            => PatchHelper.Log(message(Path));

        internal void LogSyncFailed(Exception ex)
            => PatchHelper.Log(SyncFailed(Path, ex));

        private async Task PullCloudOnlyFileAsync()
        {
            string cloud = await ReadCloudContentAsync(PullCloudFileOperation);
            await PullCloudContentAsync(cloud, PullDownloaded, backUpLocal: false);
        }

        private void PushLocalOnlyFile(string localContent)
        {
            PushLocalContent(localContent, cloudContent: null, PushUploaded);
        }

        internal async Task PullCloudContentAsync(
            string content,
            Func<string, string> message,
            bool backUpLocal
        )
        {
            Log(message);
            if (backUpLocal)
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

        private bool LocalFileExists()
            => _local.FileExists(Path);

        internal async Task RunAsync()
        {
            var localContent = ReadLocalContent();
            var cloud = CloudFileExists();

            if (cloud && localContent != null)
            {
                await SyncExistingFileAsync(this, localContent);
                return;
            }

            if (cloud)
            {
                await PullCloudOnlyFileAsync();
                return;
            }

            if (localContent != null)
                PushLocalOnlyFile(localContent);
        }
    }

    // Uses content comparison only because timestamps are unreliable on mobile.
    // Progress/run files compare durable progress; non-progress conflicts default to cloud.
    internal static async Task AutoSyncFileAsync(ISaveStore local, ICloudSaveStore cloud, string path)
    {
        var sync = new AutoSyncContext(local, cloud, path);
        try
        {
            await sync.RunAsync();
        }
        catch (Exception ex)
        {
            sync.LogSyncFailed(ex);
        }
    }
}
