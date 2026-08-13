using System;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void ShowConfirmation(string message, Action onConfirmed)
    {
        _parent.AddChild(BuildConfirmationDialog(message, CurrentConfirmationProfile(), onConfirmed));
    }

    internal void ShowConfirmation(
        string message,
        Action onConfirmed,
        string confirmText,
        string cancelText
    )
    {
        _parent.AddChild(BuildConfirmationDialog(
            message,
            CurrentConfirmationProfile(),
            onConfirmed,
            confirmText: confirmText,
            cancelText: cancelText
        ));
    }

    internal void ShowSaveSyncConflict(
        Action useSteamSaves,
        Action useDeviceSaves
    )
        => ShowConfirmation(
            "Saves changed on this device and in Steam Cloud. Choose which copy to keep; the other copy will be replaced. Vanilla and modded saves remain separate.",
            useDeviceSaves,
            useSteamSaves,
            confirmText: "Use this device",
            cancelText: "Use Steam saves"
        );

    internal void ShowGetSavesOverwriteConfirmation(
        Action onConfirmed,
        Action onCancelled
    )
        => ShowConfirmation(
            "Steam saves differ from this device. Getting them will replace or delete vanilla and modded saves on this device. Continue?",
            onConfirmed,
            onCancelled,
            confirmText: "Get saves from Steam",
            cancelText: "Cancel"
        );

    internal void ShowSendSavesOverwriteConfirmation(
        Action onConfirmed,
        Action onCancelled
    )
        => ShowConfirmation(
            "This device differs from Steam. Sending these saves will replace or delete vanilla and modded saves in Steam Cloud. Continue?",
            onConfirmed,
            onCancelled,
            confirmText: "Send saves to Steam",
            cancelText: "Cancel"
        );

    internal void ShowConfirmation(string message, Action onConfirmed, Action onCancelled)
    {
        _parent.AddChild(BuildConfirmationDialog(
            message,
            CurrentConfirmationProfile(),
            onConfirmed,
            onCancelled
        ));
    }

    internal void ShowConfirmation(
        string message,
        Action onConfirmed,
        Action onCancelled,
        string confirmText,
        string cancelText
    )
    {
        _parent.AddChild(BuildConfirmationDialog(
            message,
            CurrentConfirmationProfile(),
            onConfirmed,
            onCancelled,
            confirmText,
            cancelText
        ));
    }

    private LauncherLayoutProfile CurrentConfirmationProfile()
    {
        var viewportSize = _parent.GetViewport()?.GetVisibleRect().Size ?? _profile.ViewportSize;
        return viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize, _profile.TouchOptimized)
            : _profile;
    }
}
