namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    internal void AutoLaunchRequested(bool safeLaunch)
        => LaunchAfterSaveSync(() => _launch.AutoLaunchRequested(safeLaunch));
}
