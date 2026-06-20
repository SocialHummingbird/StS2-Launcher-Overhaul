namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void CloudSyncToggled(bool pressed)
    {
        LauncherPreferences.SaveCloudSyncEnabled(pressed);
        _view.SetStatus(
            pressed
                ? "Game cloud sync enabled. Manual Push/Pull remains available from the launcher."
                : "Game cloud sync disabled. The game will use Android local saves; manual Push/Pull remains available."
        );
    }
}
