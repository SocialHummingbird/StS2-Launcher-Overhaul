using System;
using System.Threading.Tasks;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherSteamSession
{
    private const int GuardCodePollDelayMs = 500;

    internal void SubmitCode(string code)
    {
        if (!_awaitingCode)
            return;

        _codeTcs?.TrySetResult(code);
    }

    private async Task<string> RequestSteamGuardCodeAsync(
        bool wasIncorrect,
        Action<bool> codeNeeded
    )
    {
        var codeTask = BeginSteamGuardCodeRequest(wasIncorrect, codeNeeded);

        try
        {
            return await WaitForSteamGuardCodeAsync(codeTask);
        }
        finally
        {
            EndSteamGuardCodeRequest();
        }
    }

    private Task<string> BeginSteamGuardCodeRequest(
        bool wasIncorrect,
        Action<bool> codeNeeded
    )
    {
        _codeTcs = new TaskCompletionSource<string>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _awaitingCode = true;
        codeNeeded?.Invoke(wasIncorrect);
        return _codeTcs.Task;
    }

    private void EndSteamGuardCodeRequest()
    {
        ClearPendingCodeRequest();
    }

    private void ClearPendingCodeRequest()
    {
        var codeTcs = _codeTcs;
        _codeTcs = null;
        _awaitingCode = false;
        codeTcs?.TrySetCanceled();
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
