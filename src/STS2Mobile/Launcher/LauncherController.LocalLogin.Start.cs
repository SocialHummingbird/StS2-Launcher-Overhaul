using System;
using System.Threading;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
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
}
