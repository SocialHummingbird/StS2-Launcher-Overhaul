using Godot;
using System;

namespace STS2Mobile.Launcher;
internal sealed partial class LauncherView
{
    internal void UpdateViewportSize(Vector2 viewportSize)
    {
        _panelBaseY = _panel.Position.Y + _keyboardOffset;
        _panel.UpdateSizeFromViewport(viewportSize, _profile.PanelHeightRatio);
        UpdateCompactStickyTaskHeader(viewportSize);
        UpdateCompactSectionResponsiveRows(viewportSize);
        UpdateDiagnosticsLogViewport(viewportSize);
        UpdateKeyboardOffset();
        RequestAndroidCompositionRefresh();
    }

    internal void ShowConfirmation(string message, Action onConfirmed)
    {
        _parent.AddChild(BuildConfirmationDialog(message, CurrentConfirmationProfile(), onConfirmed));
    }

    internal void ShowConfirmation(string message, Action onConfirmed, string confirmText, string cancelText)
    {
        _parent.AddChild(BuildConfirmationDialog(message, CurrentConfirmationProfile(), onConfirmed, confirmText: confirmText, cancelText: cancelText));
    }

    internal void ShowSaveSyncConflict(Action useSteamSaves, Action useDeviceSaves) => ShowConfirmation("Saves changed on this device and in Steam Cloud. Choose which copy to keep; the other copy will be replaced. Vanilla and modded saves remain separate.", useDeviceSaves, useSteamSaves, confirmText: "Use this device", cancelText: "Use Steam saves");
    internal void ShowGetSavesOverwriteConfirmation(Action onConfirmed, Action onCancelled) => ShowConfirmation("Steam saves differ from this device. Getting them will replace or delete vanilla and modded saves on this device. Continue?", onConfirmed, onCancelled, confirmText: "Get saves from Steam", cancelText: "Cancel");
    internal void ShowSendSavesOverwriteConfirmation(Action onConfirmed, Action onCancelled) => ShowConfirmation("This device differs from Steam. Sending these saves will replace or delete vanilla and modded saves in Steam Cloud. Continue?", onConfirmed, onCancelled, confirmText: "Send saves to Steam", cancelText: "Cancel");
    internal void ShowConfirmation(string message, Action onConfirmed, Action onCancelled)
    {
        _parent.AddChild(BuildConfirmationDialog(message, CurrentConfirmationProfile(), onConfirmed, onCancelled));
    }

    internal void ShowConfirmation(string message, Action onConfirmed, Action onCancelled, string confirmText, string cancelText)
    {
        _parent.AddChild(BuildConfirmationDialog(message, CurrentConfirmationProfile(), onConfirmed, onCancelled, confirmText, cancelText));
    }

    private LauncherLayoutProfile CurrentConfirmationProfile()
    {
        var viewportSize = _parent.GetViewport()?.GetVisibleRect().Size ?? _profile.ViewportSize;
        return viewportSize.X > 0f && viewportSize.Y > 0f ? LauncherLayoutProfile.ForViewport(viewportSize, _profile.TouchOptimized) : _profile;
    }

    internal void UpdateKeyboardOffset()
    {
        var kbHeight = DisplayServer.VirtualKeyboardGetHeight();
        if (kbHeight > 0)
        {
            var windowSize = DisplayServer.WindowGetSize();
            var vpSize = _parent.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920, 1080);
            var scale = windowSize.Y > 0 ? vpSize.Y / windowSize.Y : 1f;
            var maxOffset = Math.Max(0f, vpSize.Y * 0.42f);
            _keyboardOffset = Math.Min(kbHeight * scale * 0.85f, maxOffset);
            _panel.Position = new Vector2(_panel.Position.X, _panelBaseY - _keyboardOffset);
            ScrollFocusedInputAboveKeyboard();
            return;
        }

        _keyboardOffset = 0f;
        _keyboardFocusScrollTarget = null;
        _keyboardFocusScrollOffset = -1f;
        _panel.Position = new Vector2(_panel.Position.X, _panelBaseY);
    }

    private void ScrollFocusedInputAboveKeyboard()
    {
        var focusOwner = _parent.GetViewport()?.GuiGetFocusOwner();
        if (focusOwner == null || !PrimaryScroll.IsAncestorOf(focusOwner))
            return;
        if (focusOwner == _keyboardFocusScrollTarget && Math.Abs(_keyboardFocusScrollOffset - _keyboardOffset) < 1f)
            return;
        _keyboardFocusScrollTarget = focusOwner;
        _keyboardFocusScrollOffset = _keyboardOffset;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(PrimaryScroll) || !GodotObject.IsInstanceValid(focusOwner) || !PrimaryScroll.IsInsideTree() || !focusOwner.IsInsideTree())
            {
                return;
            }

            PrimaryScroll.EnsureControlVisible(focusOwner);
        }).CallDeferred();
    }

    private void DismissKeyboard(InputEvent ev)
    {
        if (ev is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true })
        {
            _parent.GetViewport()?.GuiReleaseFocus();
            if (OperatingSystem.IsAndroid())
            {
                try
                {
                    DisplayServer.VirtualKeyboardHide();
                }
                catch
                {
                // Some Android backends may already have removed the IME connection.
                }

                AndroidGodotAppBridge.NotifyLauncherTextEditingRequested(false);
            }
        }
    }

    private void UpdateCompactStickyTaskHeader(Vector2 viewportSize)
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(_compactStickyTaskHeader) || !GodotObject.IsInstanceValid(_compactCurrentTaskButton) || !GodotObject.IsInstanceValid(_compactWorkflowStrip))
        {
            return;
        }

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f ? LauncherLayoutProfile.ForViewport(viewportSize, _profile.TouchOptimized) : _profile;
        ApplyCompactStickyTaskHeaderLayout(_compactStickyTaskHeader, _compactCurrentTaskButton, _compactWorkflowStrip, profile);
    }

    private void UpdateCompactSectionResponsiveRows(Vector2 viewportSize)
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(Code))
            return;
        var profile = viewportSize.X > 0f && viewportSize.Y > 0f ? LauncherLayoutProfile.ForViewport(viewportSize, _profile.TouchOptimized) : _profile;
        Code.UpdateViewportProfile(profile);
    }
}
