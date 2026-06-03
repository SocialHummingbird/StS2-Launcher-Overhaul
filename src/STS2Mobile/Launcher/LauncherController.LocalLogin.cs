using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private const int LocalLoginPollDelayMs = 500;

    private volatile bool _localLoginHandoffStarted;

    private void StartLocalLoginHandoff()
    {
        if (_localLoginHandoffStarted || !OperatingSystem.IsAndroid())
            return;

        _localLoginHandoffStarted = true;
        _ = Task.Run(RunLocalLoginHandoffAsync);
    }

    private async Task RunLocalLoginHandoffAsync()
    {
        try
        {
            await WatchLocalLoginHandoffAsync();
        }
        catch (Exception ex)
        {
            PatchHelper.Log($"[Launcher] Local Steam credential handoff failed: {ex}");
            _runOnMainThread(() => ShowLoginForm($"Login failed: {ex.GetBaseException().Message}"));
        }
    }

    private async Task WatchLocalLoginHandoffAsync()
    {
        while (_model.IsConnectionPending())
        {
            var localLogin = ConsumeLocalSteamCredentials();
            if (localLogin.HasValue)
            {
                PatchHelper.Log("[Launcher] Consumed local Steam credential file");
                _runOnMainThread(() => ShowSessionState(LauncherModel.SessionState.Authenticating));

                await LoginWithLocalCredentialsAsync(localLogin.Value);
                return;
            }

            await Task.Delay(LocalLoginPollDelayMs);
        }
    }

    private Task LoginWithLocalCredentialsAsync(LocalSteamCredentials credentials)
        => _model.LoginAsync(credentials.Username, credentials.Password);
}
