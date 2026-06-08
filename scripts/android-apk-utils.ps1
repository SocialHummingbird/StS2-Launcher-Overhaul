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

function Resolve-AndroidAaptPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    $sdkRoots = @()
    if (Test-Path -LiteralPath $AdbPath) {
        $platformToolsDir = Split-Path -Parent (Resolve-Path -LiteralPath $AdbPath).Path
        $sdkRoots += Split-Path -Parent $platformToolsDir
    } else {
        $adbCommand = Get-Command $AdbPath -ErrorAction SilentlyContinue
        if ($adbCommand -and $adbCommand.Source) {
            $platformToolsDir = Split-Path -Parent (Resolve-Path -LiteralPath $adbCommand.Source).Path
            $sdkRoots += Split-Path -Parent $platformToolsDir
        }
    }

    $sdkRoots += @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT)
    $sdkRoots = @($sdkRoots |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { (Resolve-Path -LiteralPath $_ -ErrorAction SilentlyContinue).Path } |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        Sort-Object -Unique)

    foreach ($sdkRoot in $sdkRoots) {
        $buildToolsDir = Join-Path $sdkRoot "build-tools"
        if (-not (Test-Path -LiteralPath $buildToolsDir)) {
            continue
        }

        $aapt = Get-ChildItem -LiteralPath $buildToolsDir -Directory |
            ForEach-Object {
                $aaptPath = $null
                foreach ($candidateName in @("aapt.exe", "aapt")) {
                    $candidatePath = Join-Path $_.FullName $candidateName
                    if (Test-Path -LiteralPath $candidatePath) {
                        $aaptPath = $candidatePath
                        break
                    }
                }

                if ($aaptPath) {
                    try {
                        $parsedVersion = [version]$_.Name
                    } catch {
                        $parsedVersion = [version]"0.0"
                    }

                    [pscustomobject]@{
                        Path = $aaptPath
                        Version = $parsedVersion
                        Name = $_.Name
                    }
                }
            } |
            Sort-Object @{ Expression = { $_.Version }; Descending = $true }, Name -Descending |
            Select-Object -First 1

        if ($aapt) {
            return $aapt.Path
        }
    }

    throw "Android SDK aapt tool not found. Set ANDROID_HOME or ANDROID_SDK_ROOT to an SDK with build-tools installed."
}

function Invoke-AndroidAaptBadging {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApkPath,
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    if (-not (Test-Path -LiteralPath $ApkPath)) {
        throw "APK not found: $ApkPath"
    }

    $aapt = Resolve-AndroidAaptPath -AdbPath $AdbPath
    $badging = @(& $aapt dump badging $ApkPath 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read APK badging with aapt: $ApkPath"
    }

    return $badging
}

function Get-AndroidApkMetadata {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApkPath,
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    $resolvedPath = (Resolve-Path -LiteralPath $ApkPath).Path
    $badging = Invoke-AndroidAaptBadging -ApkPath $resolvedPath -AdbPath $AdbPath
    $joined = $badging -join "`n"
    $packageMatch = [regex]::Match(
        $joined,
        "package:\s+name='([^']+)'\s+versionCode='([0-9]+)'\s+versionName='([^']*)'"
    )
    if (-not $packageMatch.Success) {
        throw "Could not resolve APK package metadata from badging: $resolvedPath"
    }

    $nativeCodeLine = @($badging | Where-Object { ([string]$_).StartsWith("native-code:") } | Select-Object -First 1)
    $file = Get-Item -LiteralPath $resolvedPath

    return [pscustomobject]@{
        Path = $resolvedPath
        Name = $file.Name
        PackageName = $packageMatch.Groups[1].Value
        VersionCode = [int64]$packageMatch.Groups[2].Value
        VersionName = $packageMatch.Groups[3].Value
        NativeCodeLine = [string]$nativeCodeLine
        SupportsArm64 = [bool]([string]$nativeCodeLine -match "'arm64-v8a'")
        SupportsX86_64 = [bool]([string]$nativeCodeLine -match "'x86_64'")
        LastWriteTimeUtc = $file.LastWriteTimeUtc
    }
}

function Test-AndroidApkSupportsAbi {
    param(
        [Parameter(Mandatory = $true)]
        $Metadata,
        [Parameter(Mandatory = $true)]
        [string]$Abi
    )

    switch ($Abi) {
        "arm64-v8a" { return [bool]$Metadata.SupportsArm64 }
        "x86_64" { return [bool]$Metadata.SupportsX86_64 }
        default { throw "Unsupported APK ABI filter: $Abi" }
    }
}

function Select-AndroidApk {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Directory,
        [Parameter(Mandatory = $true)]
        [string]$AdbPath,
        [string]$Filter = "StS2Launcher-v*.apk",
        [string]$TargetAbi = "",
        [string]$PackageName = ""
    )

    if (-not (Test-Path -LiteralPath $Directory)) {
        throw "APK directory not found: $Directory"
    }

    $candidates = @(
        Get-ChildItem -LiteralPath $Directory -Filter $Filter -ErrorAction SilentlyContinue |
            ForEach-Object {
                try {
                    Get-AndroidApkMetadata -ApkPath $_.FullName -AdbPath $AdbPath
                } catch {
                    Write-Host "Skipping APK with unreadable metadata: $($_.FullName) ($($_.Exception.Message))"
                    $null
                }
            } |
            Where-Object { $null -ne $_ }
    )

    if ($TargetAbi) {
        $candidates = @($candidates | Where-Object { Test-AndroidApkSupportsAbi -Metadata $_ -Abi $TargetAbi })
    }

    if ($PackageName) {
        $candidates = @($candidates | Where-Object { $_.PackageName -eq $PackageName })
    }

    $selected = @(
        $candidates |
            Sort-Object `
                @{ Expression = { $_.VersionCode }; Descending = $true }, `
                @{ Expression = { $_.LastWriteTimeUtc }; Descending = $true }, `
                @{ Expression = { $_.Name }; Descending = $true }
    ) | Select-Object -First 1

    if (-not $selected) {
        $requirements = @("filter=$Filter")
        if ($TargetAbi) { $requirements += "abi=$TargetAbi" }
        if ($PackageName) { $requirements += "package=$PackageName" }
        throw "No APK found in $Directory matching $($requirements -join ', ')."
    }

    Write-Host "Selected APK: $($selected.Name) package=$($selected.PackageName) version=$($selected.VersionName) versionCode=$($selected.VersionCode)"
    return $selected
}

function Get-AndroidApkPackageName {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ApkPath,
        [Parameter(Mandatory = $true)]
        [string]$AdbPath
    )

    return (Get-AndroidApkMetadata -ApkPath $ApkPath -AdbPath $AdbPath).PackageName
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
        "assets/dotnet_bcl/SteamKit2.dll",
        "assets/dotnet_bcl/System.Net.WebSockets.Client.dll",
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
