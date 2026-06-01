param(
    [Parameter(Mandatory = $true)]
    [string]$ApkPath,
    [ValidateSet("arm64-v8a", "x86_64", "universal")]
    [string]$Abi = "universal"
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-apk-utils.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$targetAbis = Resolve-AndroidApkTargetAbis -Abi $Abi
Test-AndroidApkContents -ApkPath $ApkPath -TargetAbis $targetAbis -TempRoot (Join-Path $root "tmp") -TempPrefix "apk-verify"

Write-Host "APK verification passed: $ApkPath"
Write-Host "Verified ABIs: $($targetAbis -join ', ')"
