function Add-SteamVersionSelectionPortalBehaviorKeyboardChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
        "refreshes keyboard offset anchor after viewport size changes" `
        @(
            "_panelBaseY = _panel\.Position\.Y \+ _keyboardOffset",
            "_panel\.UpdateSizeFromViewport",
            "UpdateKeyboardOffset\(\)",
            "ReanchorCompactScrollTargetAfterViewportChange\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Keyboard.cs" `
        "updates Android keyboard offset and panel position from the visible viewport" `
        @(
            "UpdateKeyboardOffset\(\)",
            "DisplayServer\.VirtualKeyboardGetHeight\(\)",
            "_keyboardOffset = Math\.Min",
            "_panelBaseY - _keyboardOffset",
            "_keyboardOffset = 0f",
            "_panelBaseY"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Keyboard.cs" `
        "scrolls focused managed inputs above the Android keyboard" `
        @(
            "ScrollFocusedInputAboveKeyboard",
            "GuiGetFocusOwner",
            "PrimaryScroll\.IsAncestorOf\(focusOwner\)",
            "PrimaryScroll\.EnsureControlVisible\(focusOwner\)",
            "GodotObject\.IsInstanceValid\(focusOwner\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.cs" `
        "tracks mutable keyboard offset state for viewport-aware Android keyboard avoidance" `
        @(
            "private float _panelBaseY",
            "private float _keyboardOffset",
            "private Control _keyboardFocusScrollTarget",
            "private float _keyboardFocusScrollOffset"
        )
}
