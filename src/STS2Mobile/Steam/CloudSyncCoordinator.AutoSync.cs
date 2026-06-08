using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private const int AutoSyncPerPathTimeoutMs = 30_000;
    private const string PullCloudFileOperation = "PullFile";
    private const string ReadCloudFileOperation = "ReadCloudFile";

    private readonly partial struct AutoSyncContext
    {
        private enum SavePresence
        {
            None,
            LocalOnly,
            CloudOnly,
            Both,
        }

        private readonly struct AutoSyncFiles
        {
            internal AutoSyncFiles(string? localContent, bool cloudExists)
            {
                LocalContent = localContent;
                CloudExists = cloudExists;
            }

            internal string? LocalContent { get; }
            private bool CloudExists { get; }

            internal SavePresence Presence
            {
                get
                {
                    if (LocalContent != null && CloudExists)
                        return SavePresence.Both;

                    if (LocalContent != null)
                        return SavePresence.LocalOnly;

                    return CloudExists
                        ? SavePresence.CloudOnly
                        : SavePresence.None;
                }
            }
        }

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

        private Task<string> ReadCloudContentAsync(string operation)
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

        private SaveComparison.SaveWinner GetExplicitWinner(
            string localContent,
            string cloudContent
        )
            => SaveComparison.GetExplicitWinner(Path, localContent, cloudContent);

        private void Log(Func<string, string> message)
            => PatchHelper.Log(message(Path));

        internal void LogSyncFailed(Exception ex)
            => PatchHelper.Log(SyncFailed(Path, ex));

        private bool LocalFileExists()
            => _local.FileExists(Path);

        internal async Task RunAsync()
        {
            var files = ReadFiles();
            await SyncFilesAsync(files);
        }

        private async Task SyncFilesAsync(AutoSyncFiles files)
        {
            switch (files.Presence)
            {
                case SavePresence.Both:
                    await SyncExistingFileAsync(files.LocalContent!);
                    break;
                case SavePresence.CloudOnly:
                    await PullCloudOnlyFileAsync();
                    break;
                case SavePresence.LocalOnly:
                    PushLocalOnlyFile(files.LocalContent!);
                    break;
                case SavePresence.None:
                    break;
            }
        }

        private AutoSyncFiles ReadFiles()
            => new(ReadLocalContent(), CloudFileExists());
    }

    private static bool IsCloudFileMissing(Exception ex)
        => ex is System.IO.FileNotFoundException
            || ex.Message.Contains("FileNotFound", StringComparison.OrdinalIgnoreCase);

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
