using System;
using System.Collections.Generic;
using System.Threading;
using MegaCrit.Sts2.Core.Saves;

namespace STS2Mobile.Steam;

// Launcher-only manual cloud sync plus local save backups.
internal static partial class CloudSyncCoordinator
{
    private static bool _localBackupEnabled;

    internal static void SetLocalBackupEnabled(bool enabled)
    {
        _localBackupEnabled = enabled;
    }

    internal static LocalBackupRefreshResult RefreshLocalBackup()
    {
        if (!_localBackupEnabled)
        {
            return LocalBackupRefreshResult.Skipped(
                AppPaths.HasStoragePermission()
            );
        }

        try
        {
            return SaveBackups.RefreshLocalMirror(
                CloudSaveStoreFactory.CreateLocalStore()
            );
        }
        catch (System.Exception ex)
        {
            PatchHelper.Log($"[Cloud] Automatic local backup refresh failed: {ex.Message}");
            return LocalBackupRefreshResult.Failed(ex.Message);
        }
    }

    internal static LocalBackupRefreshResult RefreshLocalBackup(
        ISaveStore local,
        string backupsRoot,
        IReadOnlyCollection<string> candidatePaths = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupsRoot);
        try
        {
            return SaveBackups.RefreshLocalMirrorAtRoot(
                local,
                backupsRoot,
                candidatePaths,
                cancellationToken
            );
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Automatic local backup refresh failed: {ex.Message}"
            );
            return LocalBackupRefreshResult.Failed(ex.Message);
        }
    }

    internal static void MirrorLocalSaveWrite(string path, byte[] content)
        => SaveBackups.MirrorLocalWrite(path, content);

    internal static void MirrorLocalSaveWrite(
        string path,
        byte[] content,
        CancellationToken cancellationToken
    )
        => SaveBackups.MirrorLocalWrite(
            path,
            content,
            cancellationToken
        );
}
