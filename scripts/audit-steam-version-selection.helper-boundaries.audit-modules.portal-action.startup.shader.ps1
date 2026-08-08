function Add-SteamVersionSelectionPortalActionStartupShaderBoundaryChecks {
    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.shader.ps1" `
        "keeps shader warmup audit contracts behind a focused orchestrator" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupShaderChecks",
            "audit-steam-version-selection.startup-warmup.shader.lifecycle.ps1",
            "audit-steam-version-selection.startup-warmup.shader.execution.ps1",
            "audit-steam-version-selection.startup-warmup.shader.ui.ps1",
            "Add-SteamVersionSelectionStartupWarmupShaderLifecycleChecks",
            "Add-SteamVersionSelectionStartupWarmupShaderExecutionChecks",
            "Add-SteamVersionSelectionStartupWarmupShaderUiChecks"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.shader.lifecycle.ps1" `
        "keeps shader warmup lifecycle and run-state checks focused" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupShaderLifecycleChecks",
            "ShaderWarmupScreen.cs",
            "ShaderWarmupScreen.Run.cs",
            "ShaderWarmupScreen.WarmupRun.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.shader.execution.ps1" `
        "keeps shader warmup execution and timing checks focused" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupShaderExecutionChecks",
            "ShaderWarmupScreen.Execution.cs",
            "ShaderWarmupScreen.Timing.cs"
        )

    Add-Check `
        "scripts\audit-steam-version-selection.startup-warmup.shader.ui.ps1" `
        "keeps shader warmup Android-readable UI and progress checks focused" `
        @(
            "function Add-SteamVersionSelectionStartupWarmupShaderUiChecks",
            "StyledProgressBar.cs",
            "ShaderWarmupScreen.UI.cs"
        )
}
