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
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
    }
}
