param(
    [string]$VersionName = "0.2.0-local",
    [int]$VersionCode = 200000,
    [string]$PackageName = "com.sts2launcher.overhaul.fork.local",
    [string]$AndroidHome = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk')",
    [string]$JavaHome = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\jdk-17')",
    [string]$GradlePath = "$(Join-Path $env:USERPROFILE '.gradle\wrapper\dists\gradle-8.14.3-all\10utluxaxniiv4wxiphsi49nj\gradle-8.14.3\bin\gradle.bat')",
    [string]$KeystorePath = "tmp\localtest.keystore",
    [string]$KeystorePassword = "android",
    [string]$KeystoreAlias = "androiddebugkey",
    [ValidateSet("arm64-v8a", "x86_64", "universal")]
    [string]$Abi = "arm64-v8a",
    [switch]$Install,
    [switch]$EvidenceDebuggable,
    [string]$DeviceSerial = ""
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-apk-utils.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$androidDir = Join-Path $root "android"
$projectPath = Join-Path $root "src\STS2Mobile\STS2Mobile.csproj"
$publishDir = Join-Path $root "src\STS2Mobile\bin\Release\net9.0\publish"
$bclDir = Join-Path $androidDir "assets\dotnet_bcl"
$upstreamPublishDir = Join-Path $root "upstream\godot-export\.godot\mono\publish\arm64"
$patcherProject = Join-Path $root "tools\SteamKitAndroidPatch\SteamKitAndroidPatch.csproj"
$patcherDll = Join-Path $root "tools\SteamKitAndroidPatch\bin\Release\net9.0\SteamKitAndroidPatch.dll"

if (-not (Test-Path -LiteralPath $AndroidHome)) {
    throw "Android SDK not found: $AndroidHome"
}

if (-not (Test-Path -LiteralPath $JavaHome)) {
    throw "JDK not found: $JavaHome"
}

if (-not (Test-Path -LiteralPath $GradlePath)) {
    throw "Gradle not found: $GradlePath"
}

if (-not (Test-Path -LiteralPath $KeystorePath)) {
    throw "Keystore not found: $KeystorePath"
}

if ($PackageName -eq "com.sts2launcher.overhaul.fork.dev" -and (Split-Path -Leaf $KeystorePath) -eq "localtest.keystore") {
    throw "Refusing to build a locally signed APK with the release/dev package name. Use the default local package name or pass a release keystore intentionally."
}

$env:ANDROID_HOME = $AndroidHome
$env:ANDROID_SDK_ROOT = $AndroidHome
$env:JAVA_HOME = $JavaHome
$env:PATH = "$JavaHome\bin;$AndroidHome\platform-tools;$AndroidHome\emulator;$AndroidHome\cmdline-tools\latest\bin;$env:PATH"

Write-Host "Publishing STS2Mobile..."
dotnet publish $projectPath -c Release
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed"
}

New-Item -ItemType Directory -Force $bclDir | Out-Null
function Copy-ManagedDependency([string]$Name) {
    $publishPath = Join-Path $publishDir $Name
    $upstreamPath = Join-Path $upstreamPublishDir $Name
    $destinationPath = Join-Path $bclDir $Name

    if (Test-Path -LiteralPath $publishPath) {
        Copy-Item -Force $publishPath $destinationPath
        return
    }

    if (Test-Path -LiteralPath $upstreamPath) {
        Copy-Item -Force $upstreamPath $destinationPath
        return
    }

    throw "Required managed dependency not found: $Name"
}

$managedDependencies = @(
    "STS2Mobile.dll",
    "SteamKit2.dll",
    "protobuf-net.dll",
    "protobuf-net.Core.dll",
    "System.IO.Hashing.dll",
    "ZstdSharp.dll",
    "0Harmony.dll",
    "GodotSharp.dll"
)

foreach ($dependency in $managedDependencies) {
    Copy-ManagedDependency $dependency
}

function Get-MonoRuntimePackageName([string]$TargetAbi) {
    if ($TargetAbi -eq "x86_64") {
        return "microsoft.netcore.app.runtime.mono.android-x64"
    }

    return "microsoft.netcore.app.runtime.mono.android-arm64"
}

function Resolve-NativeRuntimeLibrary([string]$TargetAbi, [string]$LibraryName) {
    $packageName = Get-MonoRuntimePackageName $TargetAbi
    $packageRoot = Join-Path $env:USERPROFILE ".nuget\packages\$packageName"
    $coreLibPath = Join-Path $bclDir "System.Private.CoreLib.dll"
    if (-not (Test-Path -LiteralPath $coreLibPath)) {
        throw "Managed runtime core library not staged: $coreLibPath"
    }

    $runtimeVersion = ([System.Diagnostics.FileVersionInfo]::GetVersionInfo($coreLibPath).ProductVersion -split '\+')[0]
    if (-not $runtimeVersion) {
        throw "Could not determine Microsoft.NETCore.App runtime version from $coreLibPath."
    }

    if (Test-Path -LiteralPath $packageRoot) {
        $cached = Get-ChildItem `
            -LiteralPath $packageRoot `
            -Filter $LibraryName `
            -Recurse `
            -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -like "*\$runtimeVersion\*" } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($cached) {
            return $cached.FullName
        }
    }

    $downloadDir = Join-Path $root "tmp\nuget-runtime"
    $extractDir = Join-Path $downloadDir "$packageName.$runtimeVersion"
    $packagePath = Join-Path $downloadDir "$packageName.$runtimeVersion.nupkg"
    $packageUrl = "https://api.nuget.org/v3-flatcontainer/$packageName/$runtimeVersion/$packageName.$runtimeVersion.nupkg"

    New-Item -ItemType Directory -Force $downloadDir | Out-Null

    if (Test-Path -LiteralPath $extractDir) {
        $extracted = Get-ChildItem `
            -LiteralPath $extractDir `
            -Filter $LibraryName `
            -Recurse `
            -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if ($extracted) {
            return $extracted.FullName
        }

        Remove-Item -LiteralPath $extractDir -Recurse -Force
    }

    if (-not (Test-Path -LiteralPath $packagePath)) {
        Write-Host "Downloading $packageName $runtimeVersion for native Android runtime libraries..."
        Invoke-WebRequest -Uri $packageUrl -OutFile $packagePath
    }

    $zipPath = Join-Path $downloadDir "$packageName.$runtimeVersion.zip"
    Copy-Item -LiteralPath $packagePath -Destination $zipPath -Force
    try {
        Expand-Archive -LiteralPath $zipPath -DestinationPath $extractDir -Force -ErrorAction Stop
    } catch {
        Write-Host "Cached $packageName $runtimeVersion package was invalid. Downloading it again..."
        if (Test-Path -LiteralPath $packagePath) {
            Remove-Item -LiteralPath $packagePath -Force
        }
        if (Test-Path -LiteralPath $zipPath) {
            Remove-Item -LiteralPath $zipPath -Force
        }
        if (Test-Path -LiteralPath $extractDir) {
            Remove-Item -LiteralPath $extractDir -Recurse -Force
        }
        Invoke-WebRequest -Uri $packageUrl -OutFile $packagePath
        Copy-Item -LiteralPath $packagePath -Destination $zipPath -Force
        Expand-Archive -LiteralPath $zipPath -DestinationPath $extractDir -Force -ErrorAction Stop
    }

    $downloaded = Get-ChildItem `
        -LiteralPath $extractDir `
        -Filter $LibraryName `
        -Recurse |
        Select-Object -First 1

    if (-not $downloaded) {
        throw "Downloaded $packageName $runtimeVersion, but $LibraryName was not found."
    }

    return $downloaded.FullName
}

$nativeRuntimeLibraries = @(
    "libSystem.Globalization.Native.so",
    "libSystem.IO.Compression.Native.so",
    "libSystem.Native.so",
    "libSystem.Security.Cryptography.Native.Android.so",
    "libmono-component-debugger.so",
    "libmono-component-diagnostics_tracing.so",
    "libmono-component-hot_reload.so",
    "libmono-component-marshal-ilgen.so",
    "libmonosgen-2.0.so"
)

foreach ($targetAbi in (Resolve-AndroidApkTargetAbis -Abi $Abi)) {
    $nativeLibDir = Join-Path $androidDir "libs\release\$targetAbi"
    New-Item -ItemType Directory -Force $nativeLibDir | Out-Null

    foreach ($library in $nativeRuntimeLibraries) {
        $nativeSo = Resolve-NativeRuntimeLibrary $targetAbi $library
        Copy-Item -Force $nativeSo (Join-Path $nativeLibDir $library)
    }
}

Write-Host "Building SteamKit Android patcher..."
dotnet build $patcherProject -c Release
if ($LASTEXITCODE -ne 0) {
    throw "SteamKit Android patcher build failed"
}

Write-Host "Patching SteamKit2.dll and System.Net.WebSockets.Client.dll Android crypto calls..."
$patchOutput = & dotnet $patcherDll (Join-Path $bclDir "SteamKit2.dll") (Join-Path $bclDir "STS2Mobile.dll") 2>&1
$patchOutput | ForEach-Object { Write-Host $_ }
if ($LASTEXITCODE -ne 0) {
    throw "SteamKit Android patcher failed with exit code $LASTEXITCODE."
}
if (-not (($patchOutput | Out-String) -match "Patched System\.Net\.WebSockets\.Client\.dll: sha1TryHashData=[0-9]+")) {
    throw "System.Net.WebSockets.Client.dll SHA1 patch summary was not emitted."
}
if ($LASTEXITCODE -ne 0) {
    throw "SteamKit Android patch failed"
}

$resolvedKeystore = (Resolve-Path $KeystorePath).Path
$targetAbis = Resolve-AndroidApkTargetAbis -Abi $Abi
$gradleAbiList = $targetAbis -join ","

Write-Host "Stopping existing Gradle daemons..."
& $GradlePath "--stop" | Out-Null

Write-Host "Building Android APK..."
$gradleArgs = @(
    "-p", "$androidDir",
    "assembleMonoRelease",
    "-Pexport_version_name=$VersionName",
    "-Pexport_version_code=$VersionCode",
    "-Pexport_package_name=$PackageName",
    "-Pexport_enabled_abis=$gradleAbiList",
    "-Prelease_keystore_file=$resolvedKeystore",
    "-Prelease_keystore_password=$KeystorePassword",
    "-Prelease_keystore_alias=$KeystoreAlias"
)
if ($EvidenceDebuggable) {
    $gradleArgs += "-Psts2_evidence_debuggable=true"
}

& $GradlePath @gradleArgs

if ($LASTEXITCODE -ne 0) {
    throw "Gradle build failed"
}

$apk = Get-ChildItem -LiteralPath (Join-Path $androidDir "build\outputs\apk\mono\release") -Filter "StS2Launcher-v*.apk" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $apk) {
    throw "APK not found after build."
}

if ($apk.Name -ne "StS2Launcher-v$VersionName.apk") {
    throw "Unexpected APK output after build. Expected StS2Launcher-v$VersionName.apk, got $($apk.Name)"
}

Write-Host "APK built: $($apk.FullName)"
Test-AndroidApkContents -ApkPath $apk.FullName -TargetAbis $targetAbis -TempRoot (Join-Path $root "tmp") -TempPrefix "apk-verify"
Write-Host "APK verification passed for ABIs: $($targetAbis -join ', ')"
& (Join-Path $PSScriptRoot "verify-android-apk-crypto-patches.ps1") -ApkPath $apk.FullName
if ($LASTEXITCODE -ne 0) {
    throw "APK Android crypto patch verification failed with exit code $LASTEXITCODE."
}

$artifactDir = Join-Path $root "artifacts\android"
$safeVersionName = $VersionName -replace '[^A-Za-z0-9._-]', '_'
$artifactAbiName = $Abi
$archivedApk = Join-Path $artifactDir "StS2Launcher-v$safeVersionName-$artifactAbiName.apk"
New-Item -ItemType Directory -Force $artifactDir | Out-Null
Copy-Item -LiteralPath $apk.FullName -Destination $archivedApk -Force
Write-Host "APK archived: $archivedApk"

$hash = Get-FileHash -Algorithm SHA256 -LiteralPath $archivedApk
$checksumPath = "$archivedApk.sha256"
"$($hash.Hash.ToLowerInvariant())  $(Split-Path -Leaf $archivedApk)" | Set-Content -LiteralPath $checksumPath -Encoding ASCII
Write-Host "APK checksum: $checksumPath"

$metadataPath = "$archivedApk.json"
$metadata = [ordered]@{
    versionName = $VersionName
    versionCode = $VersionCode
    packageName = $PackageName
    signingChannel = "local"
    keystore = (Split-Path -Leaf $resolvedKeystore)
    abi = $Abi
    apk = (Split-Path -Leaf $archivedApk)
    sha256 = $hash.Hash.ToLowerInvariant()
    builtAtUtc = (Get-Date).ToUniversalTime().ToString("o")
}
$metadata | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding UTF8
Write-Host "APK metadata: $metadataPath"

if ($Install) {
    $adbPath = Join-Path $AndroidHome "platform-tools\adb.exe"
    if (-not (Test-Path -LiteralPath $adbPath)) {
        throw "adb not found: $adbPath"
    }

    $adbArgs = @()
    if (-not [string]::IsNullOrWhiteSpace($DeviceSerial)) {
        $adbArgs += @("-s", $DeviceSerial)
    }

    $adbDeviceArgs = $adbArgs
    $adbArgs += @("install", "-r", $archivedApk)

    Write-Host "Installing APK on Android device..."
    & $adbPath @adbArgs
    if ($LASTEXITCODE -ne 0) {
        throw "adb install failed with exit code $LASTEXITCODE."
    }

    Write-Host "APK installed on Android device."
    $packageDump = & $adbPath @adbDeviceArgs shell dumpsys package $PackageName | Out-String
    $installedVersion = [regex]::Match($packageDump, 'versionName=([^\r\n]+)')
    $installedUpdate = [regex]::Match($packageDump, 'lastUpdateTime=([^\r\n]+)')
    if ($installedVersion.Success) {
        Write-Host "Installed package $PackageName $($installedVersion.Groups[1].Value.Trim())"
    }
    if ($installedUpdate.Success) {
        Write-Host "Installed package lastUpdateTime=$($installedUpdate.Groups[1].Value.Trim())"
    } else {
        Write-Host "Installed package metadata captured, but lastUpdateTime was not found."
    }
}

