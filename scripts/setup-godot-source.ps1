param(
    [string]$GodotDir = $(if ($env:GODOT_DIR) { $env:GODOT_DIR } else { Join-Path $PSScriptRoot "..\vendor\godot" }),
    [string]$GodotRepo = $(if ($env:GODOT_REPO) { $env:GODOT_REPO } else { "https://github.com/godotengine/godot.git" }),
    [string]$GodotRef = $(if ($env:GODOT_REF) { $env:GODOT_REF } else { "4.5.1-stable" })
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$expectedGodotCommit = "f62fdbde15035c5576dad93e586201f4d41ef0cb"
. (Join-Path $PSScriptRoot "godot-source-utils.ps1")

$GodotDir = Resolve-GodotSourceDirectory -GodotDir $GodotDir -Root $root
$venvDir = Join-Path $root "venv"

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $GodotDir) | Out-Null

if (-not (Test-Path -LiteralPath (Join-Path $GodotDir ".git"))) {
    Write-Host "Cloning Godot source: $GodotRepo#$GodotRef"
    git clone --depth 1 --branch $GodotRef $GodotRepo $GodotDir
} else {
    Write-Host "Godot source already exists at $GodotDir"
}

$godotCommitOutput = @(git -C $GodotDir rev-parse HEAD 2>&1)
$godotCommitExitCode = $LASTEXITCODE
$actualGodotCommit = ($godotCommitOutput -join "`n").Trim()
if ($godotCommitExitCode -ne 0 -or $actualGodotCommit -ne $expectedGodotCommit) {
    throw "Godot checkout is '$actualGodotCommit', expected pinned 4.5.1-stable commit $expectedGodotCommit."
}

Apply-GodotPatches -GodotDir $GodotDir -Root $root

if (-not (Test-Path -LiteralPath $venvDir)) {
    python -m venv $venvDir
}

$venvPython = Join-Path $venvDir "Scripts\python.exe"
& $venvPython -m pip install --require-hashes -r (Join-Path $root "scripts\requirements-godot-build.txt")
if ($LASTEXITCODE -ne 0) {
    throw "Failed to install hash-pinned SCons 4.10.1."
}

Write-Host ""
Write-Host "Godot source is ready."
Write-Host ""
Write-Host "For emulator work, arm64-v8a and x86_64 libgodot_android.so must be built from the same engine checkout."
Write-Host "Pinned Godot commit: $expectedGodotCommit"
Write-Host ""
Write-Host "Next:"
Write-Host "  .\scripts\build-godot.ps1"
