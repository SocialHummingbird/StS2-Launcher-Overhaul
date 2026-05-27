param(
    [string]$Repo = "SocialHummingbird/StS2-Launcher-Overhaul",
    [string]$ReleaseTag = "v0.2.88-apk-native-verify",
    [string]$AssetName = "StS2Launcher-v0.2.88-universal-phone.apk",
    [ValidateSet("arm64-v8a", "x86_64", "universal")]
    [string]$Abi = "universal",
    [string]$ArtifactsDir = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
if (-not $ArtifactsDir) {
    $ArtifactsDir = Join-Path $root "artifacts\android"
}

New-Item -ItemType Directory -Force $ArtifactsDir | Out-Null

function Get-TargetAbis {
    if ($Abi -eq "universal") {
        return @("arm64-v8a", "x86_64")
    }

    return @($Abi)
}

function Test-FileContainsAscii([string]$Path, [string]$Needle) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $pattern = [System.Text.Encoding]::ASCII.GetBytes($Needle)
    if ($pattern.Length -eq 0 -or $bytes.Length -lt $pattern.Length) {
        return $false
    }

    for ($i = 0; $i -le $bytes.Length - $pattern.Length; $i++) {
        $matched = $true
        for ($j = 0; $j -lt $pattern.Length; $j++) {
            if ($bytes[$i + $j] -ne $pattern[$j]) {
                $matched = $false
                break
            }
        }
        if ($matched) {
            return $true
        }
    }

    return $false
}

function Test-ZipEntryExists($Zip, [string]$Name) {
    return $null -ne $Zip.GetEntry($Name)
}

function Get-ReleaseAssetDigest([string]$Repo, [string]$ReleaseTag, [string]$AssetName) {
    $releaseJson = gh release view $ReleaseTag --repo $Repo --json assets
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read GitHub release: $Repo $ReleaseTag"
    }

    $release = $releaseJson | ConvertFrom-Json
    $asset = @($release.assets | Where-Object { $_.name -eq $AssetName } | Select-Object -First 1)
    if (-not $asset) {
        $names = @($release.assets | ForEach-Object { $_.name })
        throw "Release asset not found: $AssetName. Available assets: $($names -join ', ')"
    }

    return $asset.digest
}

function Verify-AndroidApk([string]$ApkPath, [string[]]$TargetAbis) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    $requiredAssets = @(
        "assets/bootstrap.pck",
        "assets/dotnet_bcl/STS2Mobile.dll",
        "assets/dotnet_bcl/GodotSharp.dll",
        "assets/dotnet_bcl/System.Private.CoreLib.dll"
    )
    $requiredNativeRuntimeLibs = @(
        "libSystem.Native.so",
        "libSystem.Security.Cryptography.Native.Android.so",
        "libmonosgen-2.0.so"
    )
    $patchedMarker = ".NET: Android platform detected. Setting api_assemblies_dir to app data path"
    $staleMarker = ".NET: Android platform detected. Setting api_assemblies_dir directly to pck path"
    $tempDir = Join-Path $root ("tmp\release-apk-verify-" + [guid]::NewGuid().ToString("N"))

    New-Item -ItemType Directory -Force $tempDir | Out-Null
    $zip = $null
    try {
        $zip = [System.IO.Compression.ZipFile]::OpenRead($ApkPath)

        foreach ($asset in $requiredAssets) {
            if (-not (Test-ZipEntryExists $zip $asset)) {
                throw "APK verification failed. Missing required asset: $asset"
            }
        }

        foreach ($targetAbi in $TargetAbis) {
            $godotEntryName = "lib/$targetAbi/libgodot_android.so"
            $godotEntry = $zip.GetEntry($godotEntryName)
            if ($null -eq $godotEntry) {
                throw "APK verification failed. Missing native Godot library: $godotEntryName"
            }

            foreach ($runtimeLib in $requiredNativeRuntimeLibs) {
                $runtimeEntryName = "lib/$targetAbi/$runtimeLib"
                if (-not (Test-ZipEntryExists $zip $runtimeEntryName)) {
                    throw "APK verification failed. Missing native runtime library: $runtimeEntryName"
                }
            }

            $extractedGodot = Join-Path $tempDir "$targetAbi-libgodot_android.so"
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($godotEntry, $extractedGodot, $true)
            if (-not (Test-FileContainsAscii $extractedGodot $patchedMarker)) {
                throw "APK verification failed. $godotEntryName does not contain the app-data assembly lookup marker."
            }
            if (Test-FileContainsAscii $extractedGodot $staleMarker) {
                throw "APK verification failed. $godotEntryName still contains the stale PCK assembly lookup marker."
            }
        }
    } finally {
        if ($zip) {
            $zip.Dispose()
        }
        if (Test-Path -LiteralPath $tempDir) {
            Remove-Item -LiteralPath $tempDir -Recurse -Force
        }
    }
}

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

$digest = Get-ReleaseAssetDigest -Repo $Repo -ReleaseTag $ReleaseTag -AssetName $AssetName
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

$targetAbis = Get-TargetAbis
Verify-AndroidApk -ApkPath $apkPath -TargetAbis $targetAbis

Write-Host "Release APK verification passed: $ReleaseTag/$AssetName"
Write-Host "Verified ABIs: $($targetAbis -join ', ')"
