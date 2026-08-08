function Add-SteamVersionSelectionPortalActionStartupShellBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.ps1" `
        "keeps startup safe-mode, shader warmup, and startup-status audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupChecks",
            "audit-steam-version-selection.startup-warmup.startup-mode.ps1",
            "audit-steam-version-selection.startup-warmup.shader.ps1",
            "audit-steam-version-selection.startup-warmup.status.ps1",
            "Add-SteamVersionSelectionStartupWarmupStartupModeChecks",
            "Add-SteamVersionSelectionStartupWarmupShaderChecks",
            "Add-SteamVersionSelectionStartupWarmupStatusChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.startup-mode.ps1" `
        "keeps startup safe-mode audit contracts focused" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupStartupModeChecks",
            "LauncherStartupFlow.StartupMode.cs",
            "LauncherStartupFlow.StartupMode.PreviousPhase.cs",
            "LauncherStartupFlow.StartupMode.SaveModePlan.cs"
        )
}
