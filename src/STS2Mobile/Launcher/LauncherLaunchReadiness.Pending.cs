namespace STS2Mobile.Launcher;

internal sealed partial class LauncherLaunchReadiness
{
    internal static LauncherLaunchReadiness Pending(
        string dataDir,
        string branch,
        string phase,
        string detail
    )
        => new(
            dataDir,
            branch,
            ready: false,
            detail,
            runtimeSlot: null,
            phase,
            LauncherLaunchReadinessCacheStatus.Pending
        );
}
