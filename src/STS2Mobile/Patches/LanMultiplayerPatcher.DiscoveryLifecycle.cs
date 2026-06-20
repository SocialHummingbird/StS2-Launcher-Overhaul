using Godot;

namespace STS2Mobile.Patches;

internal static partial class LanMultiplayerPatcher
{
    private static void StopHostBeacon()
    {
        _hostBeacon?.Stop();
        _hostBeacon = null;
    }

    private static void OpenDiscovery(object screen)
    {
        var loadingOverlay = (Control)_loadingOverlayField.GetValue(screen);
        loadingOverlay.Visible = false;

        var buttonContainer = (Control)_buttonContainerField.GetValue(screen);
        foreach (var child in buttonContainer.GetChildren())
            child.QueueFree();

        var loadingIndicator = (Control)_loadingIndicatorField.GetValue(screen);
        loadingIndicator.Visible = true;

        var noFriendsLabel = (Control)_noFriendsLabelField.GetValue(screen);
        noFriendsLabel.Visible = false;

        CloseDiscovery();
        _discovery = new LanDiscovery();
        _discovery.Start(screen, buttonContainer);
    }

    private static void CloseDiscovery()
    {
        _discovery?.Stop();
        _discovery = null;
    }
}
