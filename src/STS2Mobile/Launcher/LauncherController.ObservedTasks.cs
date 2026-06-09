using System;
using System.Threading.Tasks;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void StartObservedLauncherTask(
        string taskName,
        Func<Task> taskFactory,
        Action<Exception> handleEscapedException
    )
    {
        try
        {
            _ = ObserveLauncherTaskAsync(
                taskName,
                taskFactory(),
                handleEscapedException
            );
        }
        catch (Exception ex)
        {
            HandleEscapedLauncherTaskException(
                taskName,
                handleEscapedException,
                ex
            );
        }
    }

    private async Task ObserveLauncherTaskAsync(
        string taskName,
        Task task,
        Action<Exception> handleEscapedException
    )
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            HandleEscapedLauncherTaskException(
                taskName,
                handleEscapedException,
                ex
            );
        }
    }

    private void HandleEscapedLauncherTaskException(
        string taskName,
        Action<Exception> handleEscapedException,
        Exception ex
    )
    {
        PatchHelper.Log(
            $"[Launcher] {taskName} failed outside its normal error boundary: {ex}"
        );
        _runOnMainThread(() => handleEscapedException(ex));
    }
}
