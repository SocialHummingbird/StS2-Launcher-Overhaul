namespace STS2Mobile.Launcher;

internal static class LauncherLaunchAttemptPhases
{
    internal const string SetupFailed = "setup failed";
    internal const string Checking = "checking";
    internal const string Ready = "ready";
    internal const string Blocked = "blocked";
    internal const string BlockedInModel = "blocked in model";
    internal const string ReadinessFailed = "readiness failed";
    internal const string ModReadinessFailed = "mod readiness failed";
    internal const string InProcessSignalFailed = "in-process signal failed";
    internal const string InProcessSignalled = "in-process signalled";
    internal const string LaunchHandoffFailed = "launch handoff failed";
    internal const string LaunchHandoffNotRequested = "launch handoff not requested";
    internal const string RestartRequested = "restart requested";
    internal const string RestartRequestedWithoutReadyFiles = "restart requested without ready files";
    internal const string SafeAndroidRestartRequested = "safe android restart requested";

    internal static bool IsSuccessfulHandoffPhase(string phase)
        => string.Equals(phase, RestartRequested, System.StringComparison.Ordinal)
            || string.Equals(phase, SafeAndroidRestartRequested, System.StringComparison.Ordinal)
            || string.Equals(phase, InProcessSignalled, System.StringComparison.Ordinal);
}
