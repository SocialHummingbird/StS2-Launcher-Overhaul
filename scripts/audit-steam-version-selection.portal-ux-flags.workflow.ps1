function Add-SteamVersionSelectionPortalUxWorkflowFlagChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherPortalUxSupport.Workflow.cs" `
        "declares stable destination, safe-area, and redraw support flags" `
        @(
            "StableDestinationNavigationSupported\s*=\s*true",
            "HomeDestinationSupported\s*=\s*true",
            "SavesDestinationSupported\s*=\s*true",
            "VersionsDestinationSupported\s*=\s*true",
            "ModsDestinationSupported\s*=\s*true",
            "HelpDestinationSupported\s*=\s*true",
            "PhoneBottomNavigationSupported\s*=\s*true",
            "WideFoldableTopNavigationSupported\s*=\s*true",
            "DestinationOwnedActionGroupsSupported\s*=\s*true",
            "DeterministicDestinationScrollResetSupported\s*=\s*true",
            "DynamicTaskReanchorRemoved\s*=\s*true",
            "TouchSafeDestinationControlsSupported\s*=\s*true",
            "AndroidSafeAreaInsetsSupported\s*=\s*true",
            "AndroidBottomNavigationSafeAreaSpacerSupported\s*=\s*true",
            "AndroidCompositionRefreshSupported\s*=\s*true"
        )
}
