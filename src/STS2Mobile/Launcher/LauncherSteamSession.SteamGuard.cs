using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private const int GuardCodePollDelayMs = 500;

    internal void SubmitCode(string code) => _codeTcs?.TrySetResult(code);

    private async Task<string> RequestSteamGuardCodeAsync(
        bool wasIncorrect,
        Action<bool> codeNeeded
    )
    {
        _awaitingCode = true;
        codeNeeded?.Invoke(wasIncorrect);
        _codeTcs = new TaskCompletionSource<string>();

        try
        {
            return await WaitForSteamGuardCodeAsync(_codeTcs.Task);
        }
        finally
        {
            _awaitingCode = false;
            _codeTcs = null;
        }
    }

    private static async Task<string> WaitForSteamGuardCodeAsync(Task<string> uiCodeTask)
    {
        while (true)
        {
            if (uiCodeTask.IsCompleted)
                return await uiCodeTask;

            var localCode = TryConsumeSteamGuardCode();
            if (!string.IsNullOrWhiteSpace(localCode))
                return localCode;

            await Task.Delay(GuardCodePollDelayMs);
        }
    }
}
