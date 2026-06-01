function Resolve-AndroidApkTargetAbis {
    param(
        [ValidateSet("arm64-v8a", "x86_64", "universal")]
        [string]$Abi = "universal"
    )

    if ($Abi -eq "universal") {
        return @("arm64-v8a", "x86_64")
    }

    return @($Abi)
}

function Test-FileContainsAscii {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Needle
    )

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

function Test-ZipEntryExists {
    param(
        [Parameter(Mandatory = $true)]
        $Zip,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    return $null -ne $Zip.GetEntry($Name)
}

function Test-AndroidApkContents {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApkPath,
        [Parameter(Mandatory = $true)]
        [string[]]$TargetAbis,
        [string]$TempRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "tmp"),
        [string]$TempPrefix = "apk-verify"
    )

    if (-not (Test-Path -LiteralPath $ApkPath)) {
        throw "APK not found: $ApkPath"
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
    $tempDir = Join-Path $TempRoot ("$TempPrefix-" + [guid]::NewGuid().ToString("N"))

    New-Item -ItemType Directory -Force $tempDir | Out-Null
    $zip = $null
    try {
        $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ApkPath).Path)

        foreach ($asset in $requiredAssets) {
            if (-not (Test-ZipEntryExists -Zip $zip -Name $asset)) {
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
                if (-not (Test-ZipEntryExists -Zip $zip -Name $runtimeEntryName)) {
                    throw "APK verification failed. Missing native runtime library: $runtimeEntryName"
                }
            }

            $extractedGodot = Join-Path $tempDir "$targetAbi-libgodot_android.so"
            [System.IO.Compression.ZipFileExtensions]::ExtractToFile($godotEntry, $extractedGodot, $true)
            if (-not (Test-FileContainsAscii -Path $extractedGodot -Needle $patchedMarker)) {
                throw "APK verification failed. $godotEntryName does not contain the app-data assembly lookup marker."
            }
            if (Test-FileContainsAscii -Path $extractedGodot -Needle $staleMarker) {
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

function Get-GitHubReleaseAsset {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repo,
        [Parameter(Mandatory = $true)]
        [string]$ReleaseTag,
        [Parameter(Mandatory = $true)]
        [string]$AssetName,
        [switch]$AllowMissing
    )

    $releaseJson = gh release view $ReleaseTag --repo $Repo --json assets
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read GitHub release: $Repo $ReleaseTag"
    }

    $release = $releaseJson | ConvertFrom-Json
    $asset = @($release.assets | Where-Object { $_.name -eq $AssetName } | Select-Object -First 1)
    if (-not $asset) {
        if ($AllowMissing) {
            return $null
        }

        $names = @($release.assets | ForEach-Object { $_.name })
        throw "Release asset not found: $AssetName. Available assets: $($names -join ', ')"
    }

    return $asset
}

function Get-GitHubReleaseAssetDigest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Repo,
        [Parameter(Mandatory = $true)]
        [string]$ReleaseTag,
        [Parameter(Mandatory = $true)]
        [string]$AssetName,
        [switch]$AllowMissing
    )

    $asset = Get-GitHubReleaseAsset -Repo $Repo -ReleaseTag $ReleaseTag -AssetName $AssetName -AllowMissing:$AllowMissing
    if (-not $asset) {
        return ""
    }

    return $asset.digest
}
