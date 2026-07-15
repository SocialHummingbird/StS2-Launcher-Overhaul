function Add-SteamVersionSelectionCompactWorkflowStickyHeaderPlacementChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.Navigation.cs" `
        "builds five stable destination controls outside page content" `
        @(
            "DestinationNavigation",
            "BuildDestinationNavigation",
            "Columns = 5",
            '\["Home", "Saves", "Versions", "Mods", "Help"\]',
            "SizeFlagsHorizontal = Control\.SizeFlags\.ExpandFill",
            "FocusMode = Control\.FocusModeEnum\.All"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.cs" `
        "places destination navigation outside the scrolling page and selects Home deterministically" `
        @(
            "BuildDestinationNavigation\(profile\)",
            "shell\.Content\.AddChild\(navigation\.Root\)",
            "shell\.Content\.MoveChild\(navigation\.Root, 1\)",
            "WireDestinationNavigation\(\)",
            "SelectDestination\(LauncherDestination\.Home\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Layout.PrimaryColumn.Body.cs" `
        "builds the primary scroll container and centered body after compact sticky chrome" `
        @(
            "BuildPrimaryColumnBody",
            "new ScrollContainer",
            "leftScroll\.FollowFocus = true",
            "root\.AddChild\(leftScroll\)",
            "new MarginContainer",
            "leftFrame\.AddChild\(left\)",
            "LauncherViewLayoutMetrics\.CompactPrimaryColumnSeparation",
            "LauncherViewLayoutMetrics\.PrimaryColumnSeparation",
            "return new LauncherViewPrimaryBody"
        )
}
