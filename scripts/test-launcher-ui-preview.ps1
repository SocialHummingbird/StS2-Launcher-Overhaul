param(
    [string]$GodotPath = "",
    [string]$ImportVanillaSavesRoot = "",
    [string]$BaseGamePckPath = "",
    [string]$SteamworksNetPath = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "tools\LauncherUiPreview\LauncherUiPreview.csproj"
$runner = Join-Path $PSScriptRoot "run-launcher-ui-preview.ps1"

dotnet build $project -c Debug
if ($LASTEXITCODE -ne 0) {
    throw "Launcher UI preview build failed"
}

foreach ($profile in @(
    @{ Width = 412; Height = 915; TouchOptimized = $true },
    @{ Width = 1280; Height = 800; TouchOptimized = $false }
)) {
    $arguments = @{
        Fixture = "ready"
        Destination = "home"
        Width = $profile.Width
        Height = $profile.Height
        TouchOptimized = $profile.TouchOptimized
        InteractionTest = $true
        SkipBuild = $true
    }
    if ($GodotPath) {
        $arguments.GodotPath = $GodotPath
    }
    & $runner @arguments
}

Write-Host "Launcher mod journey wiring passed."

if ([string]::IsNullOrWhiteSpace($ImportVanillaSavesRoot)) {
    throw "The focused offline suite requires one explicit ImportVanillaSavesRoot fixture."
}
if ([string]::IsNullOrWhiteSpace($SteamworksNetPath)) {
    throw "Offline mod runtime activation requires an explicit SteamworksNetPath."
}
if ([string]::IsNullOrWhiteSpace($BaseGamePckPath)) {
    throw "Offline mod runtime activation requires an explicit BaseGamePckPath."
}

foreach ($scenario in @("vanilla", "disabled", "active")) {
    $runtimeArguments = @{
        ModRuntimeTest = $true
        ModRuntimeScenario = $scenario
        ImportVanillaSavesRoot = $ImportVanillaSavesRoot
        BaseGamePckPath = $BaseGamePckPath
        SteamworksNetPath = $SteamworksNetPath
        SkipBuild = $true
    }
    if ($GodotPath) {
        $runtimeArguments.GodotPath = $GodotPath
    }
    & $runner @runtimeArguments
}

Write-Host "Desktop ImportVanillaSaves fixture stayed unloaded for Vanilla/disabled selection and activated when enabled."
Write-Host "Scope: desktop fixtures do not prove Android mod activation or an in-game effect."
