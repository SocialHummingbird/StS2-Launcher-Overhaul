function Add-SteamVersionSelectionCompactSectionFlowChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "restores compact install section for downloads" `
        @(
            "ShowDownloadAction",
            "SetCompactReadyInstallSectionVisible\(true\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "suppresses compact install section after launch-ready" `
        @(
            "SetCompactReadyInstallSectionVisible",
            "ShowLaunchActions",
            "SetCompactReadyInstallSectionVisible\(false\)",
            "!_profile\.Compact",
            "Download\.Visible = visible"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "defines completed compact auth section suppression" `
        @(
            "HideCompactCompletedAuthSections",
            "ShowCodePrompt",
            "HideCompactCompletedAuthSections\(showCode: true\)",
            "Login\.SetFormVisible\(false, disabled: true\)",
            "Code\.Visible = showCode"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "suppresses completed compact auth sections before download work" `
        @(
            "ShowDownloadAction",
            "HideCompactCompletedAuthSections\(showCode: false\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "suppresses completed compact auth sections before play or retry work" `
        @(
            "ShowRetry",
            "ShowLaunchActions",
            "HideCompactCompletedAuthSections\(showCode: false\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Scroll.cs" `
        "defines compact active-section scrolling and anchor padding" `
        @(
            "CompactScrollAnchorTopPadding = 14",
            "ScrollCompactPrimaryTo",
            "ApplyCompactScrollAnchorPadding",
            "!_profile\.Compact",
            "Callable\.From",
            "PrimaryScroll\.EnsureControlVisible\(target\)",
            "PrimaryScroll\.ScrollVertical",
            "LauncherViewLayoutMetrics\.ScaleInt\(CompactScrollAnchorTopPadding, _scale\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Auth.cs" `
        "scrolls compact auth transitions to the active section" `
        @(
            "ScrollCompactPrimaryTo\(Login\)",
            "ScrollCompactPrimaryTo\(Code\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Download.cs" `
        "scrolls compact download transitions to the active section" `
        @(
            "ScrollCompactPrimaryTo\(Download\)",
            "SetCompactCurrentTask"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Actions.cs" `
        "scrolls compact play and retry transitions to the active section" `
        @(
            "ScrollCompactPrimaryTo\(Actions\.RetryScrollTarget\)",
            "ScrollCompactPrimaryTo\(Actions\.ReadyScrollTarget\)",
            "ScrollCompactPrimaryTo\(FirstRunGuide\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Scroll.cs" `
        "remembers the latest compact scroll anchor for viewport-change re-anchoring" `
        @(
            "ScrollCompactPrimaryTo\(Control target\)",
            "!GodotObject\.IsInstanceValid\(target\)",
            "_compactScrollAnchorTarget = target",
            "PrimaryScroll\.EnsureControlVisible\(target\)",
            "ApplyCompactScrollAnchorPadding\(target\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.cs" `
        "initializes compact workflow scroll anchors to the first-run guide" `
        @(
            "_compactScrollAnchorTarget",
            "_compactScrollAnchorTarget = FirstRunGuide",
            "_compactCurrentTaskTarget = FirstRunGuide"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.CompactWorkflow.State.cs" `
        "tracks compact scroll anchor state separately from the current task target" `
        @(
            "_compactScrollAnchorTarget",
            "_compactCurrentTaskTarget",
            "SetCompactCurrentTask",
            "_compactCurrentTaskTarget = target",
            "_compactScrollAnchorTarget = target"
        )

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
