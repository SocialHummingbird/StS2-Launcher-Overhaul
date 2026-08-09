param(
    [string]$GodotPath = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "tools\LauncherUiPreview\LauncherUiPreview.csproj"
$runner = Join-Path $PSScriptRoot "run-launcher-ui-preview.ps1"

dotnet build $project -c Debug
if ($LASTEXITCODE -ne 0) {
    throw "Launcher UI preview build failed"
}

$arguments = @{
    Fixture = "ready"
    Destination = "home"
    Width = 1280
    Height = 800
    TouchOptimized = $false
    InteractionTest = $true
    SkipBuild = $true
}
if ($GodotPath) {
    $arguments.GodotPath = $GodotPath
}
& $runner @arguments

Write-Host "Launcher navigation and action wiring passed."
