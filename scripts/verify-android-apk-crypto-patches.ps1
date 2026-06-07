param(
    [string]$ApkPath = "",
    [switch]$RebuildPatcher
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$patcherDirectory = Join-Path $root "tools\SteamKitAndroidPatch"
$patcherProject = Join-Path $patcherDirectory "SteamKitAndroidPatch.csproj"
$patcherDll = Join-Path $patcherDirectory "bin\Release\net9.0\SteamKitAndroidPatch.dll"

if (-not $ApkPath) {
    $latestApk = Get-ChildItem -LiteralPath (Join-Path $root "android\build\outputs\apk\mono\release") -Filter "StS2Launcher-v*.apk" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if (-not $latestApk) {
        throw "No APK found in android\build\outputs\apk\mono\release"
    }

    $ApkPath = $latestApk.FullName
}

if (-not (Test-Path -LiteralPath $ApkPath)) {
    throw "APK not found: $ApkPath"
}

function Test-PatcherRebuildRequired {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectDirectory,
        [Parameter(Mandatory = $true)]
        [string]$PatcherDllPath
    )

    if (-not (Test-Path -LiteralPath $PatcherDllPath)) {
        return $true
    }

    $patcherDll = Get-Item -LiteralPath $PatcherDllPath
    $newerSource = Get-ChildItem -LiteralPath $ProjectDirectory -Filter "*.cs" |
        Where-Object { $_.LastWriteTimeUtc -gt $patcherDll.LastWriteTimeUtc } |
        Select-Object -First 1

    if ($newerSource) {
        return $true
    }

    $projectFile = Get-Item -LiteralPath (Join-Path $ProjectDirectory "SteamKitAndroidPatch.csproj")
    return $projectFile.LastWriteTimeUtc -gt $patcherDll.LastWriteTimeUtc
}

if ($RebuildPatcher -or (Test-PatcherRebuildRequired -ProjectDirectory $patcherDirectory -PatcherDllPath $patcherDll)) {
    dotnet build $patcherProject -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "SteamKit Android patcher build failed with exit code $LASTEXITCODE."
    }
}

dotnet $patcherDll --verify-apk $ApkPath
if ($LASTEXITCODE -ne 0) {
    throw "APK Android crypto patch verification failed with exit code $LASTEXITCODE."
}
