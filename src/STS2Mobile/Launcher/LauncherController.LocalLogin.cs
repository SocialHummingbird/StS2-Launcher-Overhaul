using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const int LocalLoginPollDelayMs = 500;
    private static readonly TimeSpan LocalLoginPollTimeout = TimeSpan.FromSeconds(180);

    private int _localLoginHandoffStarted;

    private void StartLocalLoginHandoff()
    {
        if (!OperatingSystem.IsAndroid())
            return;

        if (Interlocked.CompareExchange(ref _localLoginHandoffStarted, 1, 0) != 0)
            return;

        StartObservedLocalLoginTask(
            "Local Steam credential handoff watcher",
            () => Task.Run(RunLocalLoginHandoffAsync)
        );
    }

    private bool TryStartImmediateLocalLoginHandoff()
    {
        if (!OperatingSystem.IsAndroid())
            return false;

        if (Volatile.Read(ref _localLoginHandoffStarted) != 0)
            return false;

        if (Interlocked.CompareExchange(ref _localLoginHandoffStarted, 1, 0) != 0)
            return false;

        var localLogin = ConsumeLocalSteamCredentials();
        if (!localLogin.HasValue)
        {
            Volatile.Write(ref _localLoginHandoffStarted, 0);
            return false;
        }

        PatchHelper.Log("[Launcher] Starting immediate local Steam credential handoff");
        _runOnMainThread(
            () => ShowSessionState(LauncherModel.SessionState.Authenticating)
        );
        StartObservedLocalLoginTask(
            "Immediate local Steam credential handoff",
            () => Task.Run(
                () => RunLocalLoginHandoffAsync(
                    localLogin.Value,
                    showAuthenticatingState: false
                )
            )
        );
        return true;
    }

    private void StartObservedLocalLoginTask(
        string taskName,
        Func<Task> taskFactory
    )
        => StartObservedLauncherTask(
            taskName,
            taskFactory,
            ex =>
            {
                Volatile.Write(ref _localLoginHandoffStarted, 0);
                LoginFormFailure.LocalCredentialHandoff().Show(this, ex);
            }
        );

    private async Task RunLocalLoginHandoffAsync()
    {
        try
        {
            await WatchLocalLoginHandoffAsync();
        }
        catch (Exception ex)
        {
            _runOnMainThread(
                () => LoginFormFailure.LocalCredentialHandoff().Show(this, ex)
            );
        }
        finally
        {
            Volatile.Write(ref _localLoginHandoffStarted, 0);
        }
    }

    private async Task RunLocalLoginHandoffAsync(
        LocalSteamCredentials localLogin,
        bool showAuthenticatingState = true
    )
    {
        try
        {
            await RunLocalLoginAsync(localLogin, showAuthenticatingState);
        }
        catch (Exception ex)
        {
            _runOnMainThread(
                () => LoginFormFailure.LocalCredentialHandoff().Show(this, ex)
            );
        }
        finally
        {
            Volatile.Write(ref _localLoginHandoffStarted, 0);
        }
    }

    private async Task WatchLocalLoginHandoffAsync()
    {
        var deadline = DateTime.UtcNow + LocalLoginPollTimeout;
        var timedOut = false;
        while (_model.IsConnectionPending())
        {
            if (DateTime.UtcNow >= deadline)
            {
                timedOut = true;
                break;
            }

            var localLogin = ConsumeLocalSteamCredentials();
            if (localLogin.HasValue)
            {
                await RunLocalLoginAsync(localLogin.Value);
                return;
            }

            await Task.Delay(LocalLoginPollDelayMs);
        }

        PatchHelper.Log(
            timedOut
                ? "[Launcher] Local Steam credential handoff watcher timed out"
                : "[Launcher] Local Steam credential handoff watcher stopped; connection no longer pending"
        );
    }

    private async Task RunLocalLoginAsync(
        LocalSteamCredentials localLogin,
        bool showAuthenticatingState = true
    )
    {
        PatchHelper.Log("[Launcher] Consumed local Steam credential file");
        if (showAuthenticatingState)
        {
            _runOnMainThread(
                () => ShowSessionState(LauncherModel.SessionState.Authenticating)
            );
        }

        await localLogin.LoginAsync(_model, StartConnectionTimeout);
    }
}
