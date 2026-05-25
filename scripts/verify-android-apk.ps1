param(
    [Parameter(Mandatory = $true)]
    [string]$ApkPath,
    [ValidateSet("arm64-v8a", "x86_64", "universal")]
    [string]$Abi = "universal"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ApkPath)) {
    throw "APK not found: $ApkPath"
}

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
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$tempDir = Join-Path $root ("tmp\apk-verify-" + [guid]::NewGuid().ToString("N"))
$targetAbis = Get-TargetAbis

New-Item -ItemType Directory -Force $tempDir | Out-Null
$zip = $null
try {
    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path $ApkPath).Path)

    foreach ($asset in $requiredAssets) {
        if (-not (Test-ZipEntryExists $zip $asset)) {
            throw "APK verification failed. Missing required asset: $asset"
        }
    }

    foreach ($targetAbi in $targetAbis) {
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

Write-Host "APK verification passed: $ApkPath"
Write-Host "Verified ABIs: $($targetAbis -join ', ')"
