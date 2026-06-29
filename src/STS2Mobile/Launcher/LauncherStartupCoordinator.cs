namespace STS2Mobile.Launcher;

internal sealed class LauncherStartupCoordinator
{
    private readonly LauncherView _view;
    private readonly LauncherVersionCoordinator _versions;

    internal LauncherStartupCoordinator(
        LauncherView view,
        LauncherVersionCoordinator versions
    )
    {
        _view = view;
        _versions = versions;
    }

    internal void InitializeActionPreferences()
    {
        _versions.RefreshGameBranchOptions();
        _view.SetActionPreferences(
            LauncherPreferences.LoadAndApplyActionPreferences()
        );
    }
}
