namespace STS2Mobile.Launcher;

internal sealed partial class LauncherController
{
    private void OnDestinationSelected(LauncherDestination destination)
    {
        if (destination == LauncherDestination.Mods)
            RefreshModsPresentation();
    }

    private void RefreshModsPresentation()
    {
        try
        {
            _view.SetModsPresentation(LauncherModsPresentationState.ReadCurrent());
        }
        catch (System.Exception ex)
        {
            // Mod presentation is advisory. A discovery/read failure must not remove Play.
            PatchHelper.Log($"[Mods] Failed to refresh launcher mod presentation: {ex.Message}");
            var selection = LauncherModSelectionState.Load();
            _view.SetModsPresentation(
                LauncherModsPresentationState.Build(
                    selection,
                    System.Array.Empty<LauncherKnownMod>(),
                    marker: null
                )
            );
        }
    }
}
