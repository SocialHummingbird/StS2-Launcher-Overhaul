using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int AutoSyncPerPathTimeoutMs = 30_000;

    private readonly struct AutoSyncContext
    {
        private readonly ISaveStore _local;
        private readonly ICloudSaveStore _cloud;

        private AutoSyncContext(ISaveStore local, ICloudSaveStore cloud, string path)
        {
            _local = local;
            _cloud = cloud;
            Path = path;
        }

        private string Path { get; }

        private bool CloudFileExists()
            => _cloud.FileExists(Path);

        private bool LocalFileExists()
            => _local.FileExists(Path);

        private string ReadLocalFile()
            => _local.ReadFile(Path);

        private Task<string> ReadCloudContentAsync(string operation)
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
            bool cloudExists = sync.CloudFileExists();
            bool localExists = sync.LocalFileExists();

            if (cloudExists && localExists)
            {
                await SyncExistingFileAsync(sync);
                return;
            }

            if (cloudExists)
            {
                await PullFileAsync(sync);
                return;
            }

            if (localExists)
                await PushFileAsync(sync);
        }
        catch (Exception ex)
        {
            PatchHelper.Log(SyncFailed(sync.Path, ex));
        }
    }
}
