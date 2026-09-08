param(
    [string]$GodotPath = 'tmp\godot-4.5.1-mono\runtime\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe',
    [switch]$VerifyBaseline
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$godot = (Resolve-Path (Join-Path $root $GodotPath)).Path
$project = Join-Path $root 'tools\LauncherUiPreview'
$runtime = Join-Path $root 'upstream\godot-export\.godot\mono\publish\arm64'
$evidence = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null

dotnet build (Join-Path $project 'LauncherUiPreview.csproj') -c Debug
if ($LASTEXITCODE -ne 0) { throw 'Godot resource probe build failed.' }

$arguments = @('--headless', '--path', $project, '--', '--asset-preload-test=true', "--managed-runtime-directory=$runtime")
if ($VerifyBaseline) {
    $baseline = Join-Path $evidence 'asset-preload-godot-baseline.log'
    & $godot @arguments '--unpatched=true' *> $baseline
    if ($LASTEXITCODE -eq 0 -or (Get-Content -Raw $baseline) -notmatch 'Threaded resource backlog exceeded its bound') {
        throw "The original game's resource burst was not reproduced. Inspect $baseline"
    }
    Write-Host 'Confirmed: the original game exceeds the Android resource backlog bound.'
}

$patched = Join-Path $evidence 'asset-preload-godot-patched.log'
& $godot @arguments *> $patched
if ($LASTEXITCODE -ne 0 -or (Get-Content -Raw $patched) -notmatch 'ASSET_PRELOAD_PASS') {
    throw "Patched resource loading failed. Inspect $patched"
}
Get-Content $patched | Select-String 'ASSET_PRELOAD_PASS'
Write-Host 'Scope: actual game AssetLoadingSession and Godot threaded resources on desktop; not Android gameplay.'
