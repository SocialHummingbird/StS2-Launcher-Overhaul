function Add-SteamVersionSelectionPortalActionStartupStatusBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.status.ps1" `
        "keeps startup-status and startup cleanup audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupStatusChecks",
            "audit-steam-version-selection.startup-warmup.status.shell.ps1",
            "audit-steam-version-selection.startup-warmup.status.android.ps1",
            "audit-steam-version-selection.startup-warmup.status.cleanup.ps1",
            "Add-SteamVersionSelectionStartupWarmupStatusShellChecks",
            "Add-SteamVersionSelectionStartupWarmupStatusAndroidChecks",
            "Add-SteamVersionSelectionStartupWarmupStatusCleanupChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.status.shell.ps1" `
        "keeps startup-status shell routing and legacy fallback checks focused" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupStatusShellChecks",
            "LauncherStartupStatus.cs",
            "LauncherStartupStatus.Legacy.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.status.android.ps1" `
        "keeps Android startup-status card composition checks focused" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupStatusAndroidChecks",
            "LauncherStartupStatus.Android.cs",
            "LauncherStartupStatus.Android.Metrics.cs",
            "LauncherStartupStatus.Android.Labels.cs",
            "LauncherStartupStatus.Android.Style.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.status.cleanup.ps1" `
        "keeps startup-status cleanup checks focused" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupStatusCleanupChecks",
            "LauncherStartupStatus.cs",
            "LauncherGameStartupRecovery.State.cs"
        )
}
