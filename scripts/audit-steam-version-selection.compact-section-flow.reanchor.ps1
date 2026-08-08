function Add-SteamVersionSelectionCompactSectionFlowReanchorChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherView.Behavior.cs" `
        "updates layout without restoring a stale deferred scroll anchor" `
        @(
            "UpdateViewportSize\(Vector2 viewportSize\)",
            "UpdateKeyboardOffset\(\)",
            "RequestAndroidCompositionRefresh\(\)"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\Sections\ActionSection.Destinations.cs" `
        "keeps destination containers stable and switches visibility without runtime reparenting" `
        @(
            "BuildDestinationLayout\(\)",
            "SetDestination\(LauncherDestination destination\)",
            "ApplyDestinationVisibility\(\)",
            "_homeDestination\.Visible = _destination == LauncherDestination\.Home",
            "_savesDestination\.Visible = _destination == LauncherDestination\.Saves",
            "_versionsDestination\.Visible = _destination == LauncherDestination\.Versions",
            "_modsDestination\.Visible = _destination == LauncherDestination\.Mods",
            "_helpDestination\.Visible = _destination == LauncherDestination\.Help"
        )
}
