function Add-SteamVersionSelectionCompactSectionFlowScrollingChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Sections.Scroll.cs" `
        "keeps compact active-task transitions at a stable page origin" `
        @(
            "ScrollCompactPrimaryTo",
            "!_profile\.Compact",
            "GodotObject\.IsInstanceValid\(target\)",
            "_compactScrollAnchorTarget = target",
            "PrimaryScroll\.ScrollVertical = 0"
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
        "avoids deferred EnsureControlVisible re-anchoring during destination changes" `
        @(
            "ScrollCompactPrimaryTo\(Control target\)",
            "!GodotObject\.IsInstanceValid\(target\)",
            "_compactScrollAnchorTarget = target",
            "PrimaryScroll\.ScrollVertical = 0"
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
}
