using System;
using System.Threading;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
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
}
