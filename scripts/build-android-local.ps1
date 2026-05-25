param(
    [string]$VersionName = "0.2.0-local",
    [int]$VersionCode = 200000,
    [string]$PackageName = "com.sts2launcher.overhaul.fork.dev",
    [string]$AndroidHome = "C:\Users\ap010\.w40k-android-toolchain\android-sdk",
    [string]$JavaHome = "C:\Users\ap010\.w40k-android-toolchain\jdk-17",
    [string]$GradlePath = "C:\Users\ap010\.gradle\wrapper\dists\gradle-8.14.3-all\10utluxaxniiv4wxiphsi49nj\gradle-8.14.3\bin\gradle.bat",
    [string]$KeystorePath = "tmp\localtest.keystore",
    [string]$KeystorePassword = "android",
    [string]$KeystoreAlias = "androiddebugkey"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$androidDir = Join-Path $root "android"
$projectPath = Join-Path $root "src\STS2Mobile\STS2Mobile.csproj"
$publishDir = Join-Path $root "src\STS2Mobile\bin\Release\net9.0\publish"
$bclDir = Join-Path $androidDir "assets\dotnet_bcl"
$nativeLibDir = Join-Path $androidDir "libs\release\arm64-v8a"
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
New-Item -ItemType Directory -Force $nativeLibDir | Out-Null

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

function Resolve-NativeCryptoLibrary {
    $packageRoot = Join-Path $env:USERPROFILE ".nuget\packages\microsoft.netcore.app.runtime.mono.android-arm64"
    if (Test-Path -LiteralPath $packageRoot) {
        $cached = Get-ChildItem `
            -LiteralPath $packageRoot `
            -Filter "libSystem.Security.Cryptography.Native.Android.so" `
            -Recurse `
            -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($cached) {
            return $cached.FullName
        }
    }

    $runtimeVersion = (& dotnet --list-runtimes | Select-String -Pattern '^Microsoft\.NETCore\.App ' | Select-Object -First 1).ToString().Split(' ')[1]
    if (-not $runtimeVersion) {
        throw "Could not determine Microsoft.NETCore.App runtime version."
    }

    $packageName = "microsoft.netcore.app.runtime.mono.android-arm64"
    $downloadDir = Join-Path $root "tmp\nuget-runtime"
    $extractDir = Join-Path $downloadDir "extract"
    $packagePath = Join-Path $downloadDir "$packageName.$runtimeVersion.nupkg"
    $packageUrl = "https://api.nuget.org/v3-flatcontainer/$packageName/$runtimeVersion/$packageName.$runtimeVersion.nupkg"

    New-Item -ItemType Directory -Force $downloadDir | Out-Null
    if (Test-Path -LiteralPath $extractDir) {
        Remove-Item -LiteralPath $extractDir -Recurse -Force
    }

    Write-Host "Downloading $packageName $runtimeVersion for native Android crypto library..."
    Invoke-WebRequest -Uri $packageUrl -OutFile $packagePath
    $zipPath = Join-Path $downloadDir "$packageName.$runtimeVersion.zip"
    Copy-Item -LiteralPath $packagePath -Destination $zipPath -Force
    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractDir -Force

    $downloaded = Get-ChildItem `
        -LiteralPath $extractDir `
        -Filter "libSystem.Security.Cryptography.Native.Android.so" `
        -Recurse |
        Select-Object -First 1

    if (-not $downloaded) {
        throw "Downloaded $packageName $runtimeVersion, but libSystem.Security.Cryptography.Native.Android.so was not found."
    }

    return $downloaded.FullName
}

$cryptoSo = Resolve-NativeCryptoLibrary
if ($cryptoSo) {
    Copy-Item -Force $cryptoSo (Join-Path $nativeLibDir "libSystem.Security.Cryptography.Native.Android.so")
}

Write-Host "Building SteamKit Android patcher..."
dotnet build $patcherProject -c Release
if ($LASTEXITCODE -ne 0) {
    throw "SteamKit Android patcher build failed"
}

Write-Host "Patching SteamKit2.dll Android crypto calls..."
dotnet $patcherDll (Join-Path $bclDir "SteamKit2.dll") (Join-Path $bclDir "STS2Mobile.dll")
if ($LASTEXITCODE -ne 0) {
    throw "SteamKit Android patch failed"
}

$resolvedKeystore = (Resolve-Path $KeystorePath).Path

Write-Host "Building Android APK..."
& $GradlePath `
    "-p" "$androidDir" `
    "assembleMonoRelease" `
    "-Pexport_version_name=$VersionName" `
    "-Pexport_version_code=$VersionCode" `
    "-Pexport_package_name=$PackageName" `
    "-Prelease_keystore_file=$resolvedKeystore" `
    "-Prelease_keystore_password=$KeystorePassword" `
    "-Prelease_keystore_alias=$KeystoreAlias"

if ($LASTEXITCODE -ne 0) {
    throw "Gradle build failed"
}

$apk = Get-ChildItem -LiteralPath (Join-Path $androidDir "build\outputs\apk\mono\release") -Filter "StS2Launcher-v*.apk" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $apk) {
    throw "APK not found after build."
}

Write-Host "APK built: $($apk.FullName)"
