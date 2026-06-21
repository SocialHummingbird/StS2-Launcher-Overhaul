function Add-SteamVersionSelectionCompactSectionFlowReanchorChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.Reanchor.cs" `
        "re-anchors compact task scroll position after Android viewport changes without fighting focused keyboard input" `
        @(
            "ReanchorCompactScrollTargetAfterViewportChange\(\)",
            "DisplayServer\.VirtualKeyboardGetHeight\(\) > 0",
            "GuiGetFocusOwner",
            "PrimaryScroll\.IsAncestorOf\(focusOwner\)",
            "CompactViewportReanchorTarget",
            "IsUsableCompactAnchor",
            "ScrollCompactPrimaryTo\(target\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Visibility.cs" `
        "anchors compact ready/retry scrolling to the actual primary controls" `
        @(
            "ReadyScrollTarget",
            "_compact \? _cloudGroup : _launchButton",
            "RetryScrollTarget",
            "_retryButton"
        )
}
