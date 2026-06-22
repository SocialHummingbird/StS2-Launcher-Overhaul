using System;
using STS2Mobile.Patches;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherUI
{
    private void AutoLaunchIfRequested(bool automationStarted)
    {
        if (!_inGameMode)
            return;

        if (automationStarted)
        {
            Environment.SetEnvironmentVariable(AutoLaunchVariable, "0");
            Environment.SetEnvironmentVariable(AutoSafeLaunchVariable, "0");
            PatchHelper.Log("Auto-launch suppressed while launcher automation is running.");
            return;
        }

        if (
            !string.Equals(
                Environment.GetEnvironmentVariable(AutoLaunchVariable),
                "1",
                StringComparison.Ordinal
            )
        )
            return;

        Environment.SetEnvironmentVariable(AutoLaunchVariable, "0");
        var safeLaunch = string.Equals(
            Environment.GetEnvironmentVariable(AutoSafeLaunchVariable),
            "1",
            StringComparison.Ordinal
        );
        Environment.SetEnvironmentVariable(AutoSafeLaunchVariable, "0");
        PatchHelper.Log(
            safeLaunch
                ? "Auto-safe-launching downloaded game from launch request."
                : "Auto-launching downloaded game from launch request."
        );
        if (safeLaunch)
            _model.LaunchSafe();
        else
            _model.Launch();
    }
}
