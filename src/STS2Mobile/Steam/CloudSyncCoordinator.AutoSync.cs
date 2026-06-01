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

        public AutoSyncContext(ISaveStore local, ICloudSaveStore cloud, string path)
        {
            _local = local;
            _cloud = cloud;
            Path = path;
        }

        public string Path { get; }

        public (bool Local, bool Cloud) GetPresence()
            => (Local: _local.FileExists(Path), Cloud: _cloud.FileExists(Path));

        public bool CloudFileExists()
            => _cloud.FileExists(Path);

        public string? ReadLocalContent()
            => _local.FileExists(Path) ? _local.ReadFile(Path) : null;

        public Task<string> ReadCloudContentAsync(string operation)
            => CloudSyncCoordinator.ReadCloudContentAsync(
                _cloud,
                Path,
                operation,
                AutoSyncPerPathTimeoutMs
            );

        private Task WriteCloudContentAsync(string content)
            => CloudSyncCoordinator.WriteCloudContentAsync(
                _local,
                _cloud,
                Path,
                content,
                AutoSyncPerPathTimeoutMs
            );

        private void WriteCloudFile(string content)
            => _cloud.WriteFile(Path, content);

        public async Task PullCloudContentAsync(
            string content,
            string message,
            bool backUpLocal
        )
        {
            PatchHelper.Log(message);
            if (backUpLocal)
                BackUpLocalProgress();
            await WriteCloudContentAsync(content);
        }

        public void PushLocalContent(string localContent, string? cloudContent, string message)
        {
            PatchHelper.Log(message);
            if (cloudContent != null)
                BackUpCloudProgress(cloudContent);
            WriteCloudFile(localContent);
        }

        private void BackUpLocalProgress()
            => SaveBackups.LocalProgressFile(_local, Path);

        private void BackUpCloudProgress(string content)
            => SaveBackups.CloudProgressContent(Path, content);
    }

    // Uses content comparison only because timestamps are unreliable on mobile.
    // Progress/run files compare durable progress; non-progress conflicts default to cloud.
    internal static async Task AutoSyncFileAsync(ISaveStore local, ICloudSaveStore cloud, string path)
    {
        var sync = new AutoSyncContext(local, cloud, path);
        try
        {
            var presence = sync.GetPresence();

            if (presence.Cloud && presence.Local)
            {
                await SyncExistingFileAsync(sync);
                return;
            }

            if (presence.Cloud)
            {
                await PullFileAsync(sync);
                return;
            }

            if (presence.Local)
                await PushFileAsync(sync);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(SyncFailed(sync.Path, ex));
        }
    }
}
