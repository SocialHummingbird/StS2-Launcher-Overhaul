function Add-SteamVersionSelectionPortalBehaviorChecks {
    Add-Check `
        "src\STS2Mobile\Steam\SteamConnectionConfigurationFactory.cs" `
        "sanitizes SteamKit debug logs before writing launcher diagnostics" `
        @(
            "SanitizeSteamKitDebugMessage",
            "SteamKitDebugLogsSanitized\s*=\s*true",
            "SteamKitDebugLogsOptInEnabled",
            "STS2_STEAMKIT_DEBUG_LOGS",
            "disabled by default",
            "SensitiveJsonValueRegex",
            "SensitiveKeyValueRegex",
            "BearerTokenRegex",
            "<redacted>",
            "PatchHelper\.Log",
            "SteamKit"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "suppresses compact first-run safe-flow guidance during active auth states" `
        @(
            "SetFirstRunGuideVisible\(false\)",
            "SetLoginFormVisible\(bool visible, bool disabled\)[\s\S]*SetFirstRunGuideVisible\(false\)[\s\S]*HideCompactCompletedAuthSections",
            "ShowCodePrompt\(bool wasIncorrect\)[\s\S]*SetFirstRunGuideVisible\(false\)",
            "SetLoginFormVisible",
            "FirstRunGuide\.Visible = !_profile\.Compact \|\| visible"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "suppresses compact first-run safe-flow guidance during active download states" `
        @(
            "ShowDownloadAction\(string buttonText\)[\s\S]*SetFirstRunGuideVisible\(false\)",
            "ShowDownloadAction"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "restores compact setup guidance only when no active action section is shown" `
        @(
            "SetFirstRunGuideVisible\(true\)",
            "SetFirstRunGuideVisible\(false\)",
            "ShowLaunchActions",
            "ShowRetry",
            "HideActions"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.FmodAttribution.cs" `
        "keeps compact FMOD attribution low-profile without expanding the phone layout" `
        @(
            "BuildFmodAttributionSection\(float scale, bool compact\)",
            "Control\.SizeFlags\.ShrinkBegin",
            "Control\.SizeFlags\.ExpandFill",
            "if \(!compact\)",
            "CompactFmodCreditFontSize",
            "AutowrapMode"
        )

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

    Add-Check `
        "src\STS2Mobile\Launcher\Components\StyledPanel.cs" `
        "uses a framed launcher shell rather than a flat unbounded panel" `
        @(
            "PanelBackground",
            "BorderColor",
            "SetBorderWidthAll",
            "PanelRadius"
        )

    Add-Check `
        "src\STS2Mobile\ModEntry.StandaloneLauncher.cs" `
        "suppresses raw startup fallback banner behind the launcher portal" `
        @(
            "Startup fallback raw banner suppressed",
            "launcher diagnostics retain the startup failure detail"
        )

    Add-Check `
        "android\src\com\game\sts2launcher\GodotApp.java" `
        "keeps SteamKit debug logging opt-in at the Android boundary" `
        @(
            "ENV_STEAMKIT_DEBUG_LOGS",
            "sts2_steamkit_debug_logs",
            "setSteamKitDebugLogMode",
            "Sanitized SteamKit debug logging enabled",
            "STS2_STEAMKIT_DEBUG_LOGS"
        )
}
