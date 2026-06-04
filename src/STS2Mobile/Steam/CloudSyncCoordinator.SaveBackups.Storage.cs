using System;

namespace STS2Mobile.Steam;

internal static partial class CloudSyncCoordinator
{
    private static partial class SaveBackups
    {
        private static bool SaveContent(string path, string content, string source)
            => SaveBackup(BackupWriteRequest.Standard(path, content, source));

        private static bool SaveBackup(BackupWriteRequest request)
        {
            try
            {
                return request.TrySave();
            }
            catch (Exception ex)
            {
                PatchHelper.Log(request.FailureMessage(ex));
                return false;
            }
        }

        private static bool HasBackupAccess()
            => _localBackupEnabled && AppPaths.HasStoragePermission();

        private static bool ShouldSkipBackup(string content)
            => string.IsNullOrEmpty(content) || !HasBackupAccess();
    }
}
