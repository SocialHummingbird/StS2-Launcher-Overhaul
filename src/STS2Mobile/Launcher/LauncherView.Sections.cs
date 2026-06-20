namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void HideAllSections()
    {
        Login.Visible = false;
        Code.Visible = false;
        Download.Visible = false;
        Actions.HideAll();
    }
}
