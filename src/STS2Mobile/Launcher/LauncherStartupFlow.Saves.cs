using System;
using System.Threading;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Saves;
using STS2Mobile.Patches;
using STS2Mobile.Steam;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static readonly TimeSpan PreloadSyncTimeout = TimeSpan.FromMinutes(3);

    private static async Task<bool> InitializeSettingsAndSavesAsync(
        StartupContext startup
    )
    {
        var loaded = false;
        await RunPreloadSaveBoundaryAsync(
            () => SynchronizeSavesBeforeLoadAsync(startup),
            () =>
            {
                startup.SetSettingsAndSavesPhase();
                PatchHelper.Log(
                    "[Save] First settings/profile read starting after pre-load synchronization"
                );
                try
                {
                    SaveManager.Instance.InitSettingsData();
                    loaded = true;
                }
                catch (Exception ex)
                {
                    startup.HandleSettingsAndSavesFailure(ex);
                }
            }
        );
        return loaded;
    }

    internal static async Task RunPreloadSaveBoundaryAsync(
        Func<Task> synchronize,
        Action loadSaves
    )
    {
        ArgumentNullException.ThrowIfNull(synchronize);
        ArgumentNullException.ThrowIfNull(loadSaves);
        await synchronize();
        loadSaves();
    }

    private static async Task SynchronizeSavesBeforeLoadAsync(
        StartupContext startup
    )
    {
        ConfigureSaveSyncForGameProcess();
        var hasService = SaveSyncService.TryGetActive(out var service);
        if (startup.ShouldSkipShaderWarmup())
        {
            if (hasService)
                service.PauseAutomaticPush();
            PatchHelper.Log(
                "[Cloud] Pre-load synchronization skipped for Safe Start; automatic Push paused"
            );
            return;
        }

        if (!hasService)
        {
            PatchHelper.Log(
                "[Cloud] Pre-load synchronization unavailable; launching local with automatic Push paused"
            );
            return;
        }

        PatchHelper.Log(
            $"[Cloud] Pre-load synchronization started ({PreloadSyncTimeout.TotalSeconds:0}s bound)"
        );
        service.PauseAutomaticPush();
        try
        {
            using var timeout = new CancellationTokenSource(PreloadSyncTimeout);
            var result = await service.SyncAsync(
                SaveSyncService.SyncRequest.Reconcile,
                overwriteConfirmed: false,
                localWritesAreStopped: true,
                cancellationToken: timeout.Token
            );
            if (result.Success && result.Outcome == SaveSyncService.SyncOutcome.Pull)
            {
                PatchHelper.Log(
                    "[Cloud] Pre-load Pull completed before first settings/profile read"
                );
                return;
            }

            if (result.Success)
            {
                PatchHelper.Log(
                    $"[Cloud] Pre-load synchronization reconciled before first settings/profile read: {result.Outcome}"
                );
                return;
            }

            PatchHelper.Log(
                $"[Cloud] Pre-load synchronization failed open; automatic Push paused: {result.Message}"
            );
        }
        catch (Exception ex)
        {
            service.PauseAutomaticPush();
            PatchHelper.Log(
                $"[Cloud] Pre-load synchronization failed open; automatic Push paused: {ex.GetType().Name}"
            );
        }
    }

    private static void ConfigureSaveSyncForGameProcess()
    {
        if (SaveSyncService.TryGetActive(out _))
            return;

        try
        {
            var credentials = new SteamCredentialStore(AppPaths.AppPrivateDataDir);
            credentials.Load();
            SaveSyncService.Configure(credentials);
            PatchHelper.Log(
                "[Cloud] Game-process synchronization service configured from saved credentials"
            );
        }
        catch (Exception ex)
        {
            PatchHelper.Log(
                $"[Cloud] Game-process synchronization setup failed open: {ex.GetType().Name}"
            );
        }
    }
}
