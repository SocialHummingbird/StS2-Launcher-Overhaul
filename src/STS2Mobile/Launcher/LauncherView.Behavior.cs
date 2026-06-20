using System;
using Godot;

namespace STS2Mobile.Launcher;

internal sealed partial class LauncherView
{
    internal void UpdateViewportSize(Vector2 viewportSize)
    {
        _panelBaseY = _panel.Position.Y + _keyboardOffset;
        _panel.UpdateSizeFromViewport(viewportSize, _profile.PanelHeightRatio);
        UpdateCompactStatusHeadline(viewportSize);
        UpdateCompactStickyTaskHeader(viewportSize);
        UpdateCompactSectionResponsiveRows(viewportSize);
        UpdateDiagnosticsLogViewport(viewportSize);
        UpdateKeyboardOffset();
        ReanchorCompactScrollTargetAfterViewportChange();
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

    private void UpdateCompactStickyTaskHeader(Vector2 viewportSize)
    {
        if (!_profile.Compact
            || !GodotObject.IsInstanceValid(_compactStickyTaskHeader)
            || !GodotObject.IsInstanceValid(_compactCurrentTaskButton)
            || !GodotObject.IsInstanceValid(_compactWorkflowStrip))
        {
            return;
        }

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
        ApplyCompactStickyTaskHeaderLayout(
            _compactStickyTaskHeader,
            _compactCurrentTaskButton,
            _compactWorkflowStrip,
            profile
        );
    }

    private void UpdateCompactSectionResponsiveRows(Vector2 viewportSize)
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(Code))
            return;

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
        Code.UpdateViewportProfile(profile);
    }

    private void UpdateCompactStatusHeadline(Vector2 viewportSize)
    {
        if (!_profile.Compact
            || !GodotObject.IsInstanceValid(_compactStatusHeadline)
            || !GodotObject.IsInstanceValid(_compactStatusPhasePanel)
            || !GodotObject.IsInstanceValid(_statusActionLabel))
        {
            return;
        }

        var profile = viewportSize.X > 0f && viewportSize.Y > 0f
            ? LauncherLayoutProfile.ForViewport(viewportSize)
            : _profile;
        ApplyCompactStatusHeadlineLayout(
            _compactStatusHeadline,
            _compactStatusPhasePanel,
            _statusActionLabel,
            profile
        );
    }

    private void ReanchorCompactScrollTargetAfterViewportChange()
    {
        if (!_profile.Compact || !GodotObject.IsInstanceValid(PrimaryScroll))
            return;

        if (DisplayServer.VirtualKeyboardGetHeight() > 0)
        {
            var focusOwner = _parent.GetViewport()?.GuiGetFocusOwner();
            if (focusOwner != null && PrimaryScroll.IsAncestorOf(focusOwner))
                return;
        }

        var target = CompactViewportReanchorTarget();
        if (target == null)
            return;

        ScrollCompactPrimaryTo(target);
    }

    private Control CompactViewportReanchorTarget()
    {
        if (IsUsableCompactAnchor(_compactScrollAnchorTarget))
            return _compactScrollAnchorTarget;

        if (IsUsableCompactAnchor(_compactCurrentTaskTarget))
            return _compactCurrentTaskTarget;

        return IsUsableCompactAnchor(FirstRunGuide) ? FirstRunGuide : null;
    }

    private static bool IsUsableCompactAnchor(Control control)
        => GodotObject.IsInstanceValid(control)
            && control.IsInsideTree()
            && control.IsVisibleInTree();

    private void ScrollFocusedInputAboveKeyboard()
    {
        var focusOwner = _parent.GetViewport()?.GuiGetFocusOwner();
        if (focusOwner == null || !PrimaryScroll.IsAncestorOf(focusOwner))
            return;

        if (focusOwner == _keyboardFocusScrollTarget
            && Math.Abs(_keyboardFocusScrollOffset - _keyboardOffset) < 1f)
            return;

        _keyboardFocusScrollTarget = focusOwner;
        _keyboardFocusScrollOffset = _keyboardOffset;
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(PrimaryScroll)
                || !GodotObject.IsInstanceValid(focusOwner)
                || !PrimaryScroll.IsInsideTree()
                || !focusOwner.IsInsideTree())
            {
                return;
            }

            PrimaryScroll.EnsureControlVisible(focusOwner);
        }).CallDeferred();
    }

    private void DismissKeyboard(InputEvent ev)
    {
        if (ev is InputEventMouseButton { Pressed: true } or InputEventScreenTouch { Pressed: true })
            _parent.GetViewport()?.GuiReleaseFocus();
    }
}
