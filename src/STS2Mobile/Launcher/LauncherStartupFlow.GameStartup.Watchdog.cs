using System.Threading.Tasks;
using Godot;

namespace STS2Mobile.Launcher;

internal static partial class LauncherStartupFlow
{
    private static async Task<bool> RecoverIfWatchdogTimedOutAsync(
        Task startupTask,
        StartupContext startup,
        CanvasLayer recoveryControls
    )
    {
        var watchdogTask = Task.Delay(StartupWatchdogMs);
        if (await Task.WhenAny(startupTask, watchdogTask) != watchdogTask)
        {
            return false;
        }

        await LauncherGameStartupRecovery.HandleWatchdogAsync(
            startup.Game,
            startup.GameNode,
            startup.Status,
            recoveryControls,
            StartupWatchdogMs
        );
        return true;
    }
}
