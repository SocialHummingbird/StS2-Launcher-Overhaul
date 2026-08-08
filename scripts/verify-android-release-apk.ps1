param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [string]$ReleaseTag = "v0.2.177-login-a8729d6",
    [string]$AssetName = "StS2Launcher-v0.2.177-login-a8729d6-arm64-v8a.apk",
    [ValidateSet("arm64-v8a", "x86_64", "universal")]
    [string]$Abi = "arm64-v8a",
    [string]$ArtifactsDir = ""
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-apk-utils.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $ArtifactsDir) {
    $ArtifactsDir = Join-Path $root "artifacts\android"
}

New-Item -ItemType Directory -Force $ArtifactsDir | Out-Null

$downloadDir = Join-Path $ArtifactsDir "github-release-$ReleaseTag"
$apkPath = Join-Path $downloadDir $AssetName
New-Item -ItemType Directory -Force $downloadDir | Out-Null

Write-Host "Downloading $ReleaseTag/$AssetName from $Repo..."
gh release download $ReleaseTag --repo $Repo --pattern $AssetName --dir $downloadDir --clobber
if ($LASTEXITCODE -ne 0) {
    throw "Failed to download release asset: $ReleaseTag/$AssetName"
}

if (-not (Test-Path -LiteralPath $apkPath)) {
    throw "Downloaded APK not found: $apkPath"
}

$digest = Get-GitHubReleaseAssetDigest -Repo $Repo -ReleaseTag $ReleaseTag -AssetName $AssetName
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $apkPath).Hash.ToLowerInvariant()
if ($digest -and $digest.StartsWith("sha256:")) {
    $expected = $digest.Substring("sha256:".Length).ToLowerInvariant()
    if ($hash -ne $expected) {
        throw "APK checksum mismatch. Expected $expected from release digest, got $hash"
    }
    Write-Host "Release digest OK: $hash"
} else {
    Write-Host "Release digest unavailable; local SHA256: $hash"
}

$targetAbis = Resolve-AndroidApkTargetAbis -Abi $Abi
Test-AndroidApkContents -ApkPath $apkPath -TargetAbis $targetAbis -TempRoot (Join-Path $root "tmp") -TempPrefix "release-apk-verify"
& (Join-Path $PSScriptRoot "verify-android-apk-crypto-patches.ps1") -ApkPath $apkPath
if ($LASTEXITCODE -ne 0) {
    throw "Release APK Android crypto patch verification failed with exit code $LASTEXITCODE."
}

Write-Host "Release APK verification passed: $ReleaseTag/$AssetName"
Write-Host "Verified ABIs: $($targetAbis -join ', ')"
