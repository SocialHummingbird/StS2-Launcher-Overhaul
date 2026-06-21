function Add-SteamVersionSelectionStartupWarmupStartupModeChecks {
    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.cs" `
        "keeps startup safe-mode decisions in a small marker-driven shell" `
        @(
            "private sealed partial class StartupMode",
            "CreateFromMarkers",
            "PreviousStartupPhase\.FromMarkers",
            "ConsumeManualSafeLaunchMarker",
            "SafeLaunchRequested",
            "ShouldForceLocalSaves",
            "PhaseSettingsAndSaves",
            "PhaseGameStartup",
            "ShouldSkipShaderWarmup",
            "PhaseShaderWarmup",
            "SafeLaunchMessage"
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.PreviousPhase.cs" `
        "isolates previous startup phase marker reads and comparisons" `
        @(
            "PreviousStartupPhase",
            "LauncherLaunchMarkers\.ReadStartupPhase",
            "StringComparison\.OrdinalIgnoreCase",
            "Matches\(string phase\)",
            "DescribePreviousStall\(string message\)",
            "\$""\{message\} \{Phase\}"""
        )

    Add-Check `
        "src\STS2Mobile\Launcher\LauncherStartupFlow.StartupMode.SaveModePlan.cs" `
        "isolates startup local-save safe-mode application" `
        @(
            "StartupSaveModePlan",
            "Loading settings and saves in local-only safe mode",
            "Loading settings and saves",
            "LauncherPreferences\.LoadAndApplyCloudSyncEnabled",
            "LauncherCloudSaveState\.DisableCloudSyncForLaunch",
            "PatchHelper\.Log\(ReasonLog\)"
        )
}
