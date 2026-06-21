function Add-SteamVersionSelectionRuntimeLauncherShellBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.launcher-shell.ps1" `
        "keeps launcher shell audit contracts in a focused module" `
        @(
            "function Add-SteamVersionSelectionLauncherShellChecks",
            "LauncherUI.cs",
            "LauncherUI.Lifecycle.cs",
            "LauncherUI.MainThread.cs",
            "LauncherUI.Viewport.cs",
            "LauncherUI.AutoLaunch.cs"
        )
}
