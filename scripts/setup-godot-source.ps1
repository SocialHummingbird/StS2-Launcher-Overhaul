param(
    [string]$GodotDir = $(if ($env:GODOT_DIR) { $env:GODOT_DIR } else { Join-Path $PSScriptRoot "..\vendor\godot" }),
    [string]$GodotRepo = $(if ($env:GODOT_REPO) { $env:GODOT_REPO } else { "https://github.com/godotengine/godot.git" }),
    [string]$GodotRef = $(if ($env:GODOT_REF) { $env:GODOT_REF } else { "4.5.1-stable" })
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([System.IO.Path]::IsPathRooted($GodotDir)) {
    $GodotDir = [System.IO.Path]::GetFullPath($GodotDir)
} else {
    $GodotDir = [System.IO.Path]::GetFullPath((Join-Path $root $GodotDir))
}
$venvDir = Join-Path $root "venv"

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $GodotDir) | Out-Null

if (-not (Test-Path -LiteralPath (Join-Path $GodotDir ".git"))) {
    Write-Host "Cloning Godot source: $GodotRepo#$GodotRef"
    git clone --depth 1 --branch $GodotRef $GodotRepo $GodotDir
} else {
    Write-Host "Godot source already exists at $GodotDir"
}

if (-not (Test-Path -LiteralPath $venvDir)) {
    python -m venv $venvDir
}

$venvPython = Join-Path $venvDir "Scripts\python.exe"
& $venvPython -m pip install --upgrade pip
& $venvPython -m pip install scons

Write-Host ""
Write-Host "Godot source is ready."
Write-Host ""
Write-Host "For emulator work, arm64-v8a and x86_64 libgodot_android.so must be built from the same engine checkout."
Write-Host "If you have the original patched engine fork, rerun with GODOT_REPO and GODOT_REF set."
Write-Host ""
Write-Host "Next:"
Write-Host "  .\scripts\build-godot.ps1"
