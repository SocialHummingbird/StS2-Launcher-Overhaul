using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        internal static void CloudProgressContent(string path, string content)
            => ProgressContent(path, content, BackupSource.Cloud);

        internal static void LocalProgressFile(ISaveStore local, string path)
        {
            if (!IsProgressSave(path))
                return;

            if (!local.FileExists(path))
                return;

            ProgressContent(path, local.ReadFile(path), BackupSource.Local);
        }
    }
}
