param(
    [string]$GodotPath = "",
    [ValidateSet("signed-out", "guard", "download", "ready", "error")]
    [string]$Fixture = "ready",
    [ValidateSet("home", "saves", "versions", "mods", "help")]
    [string]$Destination = "home",
    [int]$Width = 1280,
    [int]$Height = 800,
    [bool]$TouchOptimized = $true,
    [string]$OutputPath = "",
    [switch]$SkipBuild,
    [switch]$ValidateContract
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "tools\LauncherUiPreview"
$defaultGodot = Join-Path $root (
    "tmp\godot-4.5.1-mono\runtime\Godot_v4.5.1-stable_mono_win64\" +
    "Godot_v4.5.1-stable_mono_win64_console.exe"
)

if (-not $GodotPath) {
    $GodotPath = $defaultGodot
}
if (-not (Test-Path -LiteralPath $GodotPath -PathType Leaf)) {
    throw "Godot 4.5.1 Mono was not found: $GodotPath"
}
if (-not $OutputPath) {
    $OutputPath = Join-Path $root (
        "artifacts\ui-preview\$Fixture-$Destination-$($Width)x$($Height).png"
    )
}

$resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
    $OutputPath
)
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedOutput) | Out-Null

if (-not $SkipBuild) {
    dotnet build (Join-Path $project "LauncherUiPreview.csproj") -c Debug
    if ($LASTEXITCODE -ne 0) {
        throw "Launcher UI preview build failed"
    }
}

$touch = if ($TouchOptimized) { "true" } else { "false" }
$contract = if ($ValidateContract) { "true" } else { "false" }
& $GodotPath `
    --quiet `
    --path $project `
    -- `
    "--fixture=$Fixture" `
    "--destination=$Destination" `
    "--width=$Width" `
    "--height=$Height" `
    "--touch=$touch" `
    "--validate-contract=$contract" `
    "--output=$resolvedOutput"
if ($LASTEXITCODE -ne 0) {
    throw "Launcher UI preview process failed"
}

if (-not (Test-Path -LiteralPath $resolvedOutput -PathType Leaf)) {
    throw "Launcher UI preview did not create a screenshot: $resolvedOutput"
}

Write-Host "Launcher UI preview: $resolvedOutput"
