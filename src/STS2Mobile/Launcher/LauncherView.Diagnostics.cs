namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void ShowDiagnosticsConsole()
    {
        DiagnosticsDrawer.Visible = true;
        SetDiagnosticsToggleText(DiagnosticsToggle, _profile, visible: true);
        if (_profile.Compact)
            ScrollCompactPrimaryTo(DiagnosticsDrawer);
    }
}
