using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
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
