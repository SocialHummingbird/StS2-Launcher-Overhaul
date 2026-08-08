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
        var preferences = LauncherPreferences.LoadAndApplyActionPreferences();
        var branches = _versions.ReadGameBranchOptions();
        _view.SetActionPreferences(preferences, branches);
    }
}
