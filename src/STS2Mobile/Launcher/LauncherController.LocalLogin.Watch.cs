using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
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
}
