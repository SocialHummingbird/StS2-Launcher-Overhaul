param(
    [string]$GodotPath = 'tmp\godot-4.5.1-mono\runtime\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64_console.exe',
    [switch]$VerifyBaseline,
    [switch]$AndroidMonoTransition,
    [string]$AdbPath = 'adb',
    [string]$ClangPath = 'clang++',
    [string]$DeviceSerial
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ($AndroidMonoTransition) {
    $fixture = Join-Path $root 'tmp\android-mono-transition'
    $evidence = Join-Path $root 'artifacts'
    New-Item -ItemType Directory -Force -Path $fixture, $evidence | Out-Null
    $testProject = Join-Path $root 'tests\STS2Mobile.GameIdentityTests\STS2Mobile.GameIdentityTests.csproj'
    dotnet build $testProject -c Release
    if ($LASTEXITCODE) { throw 'Android Mono fixture build failed.' }
    $testAssembly = Join-Path $root 'tests\STS2Mobile.GameIdentityTests\bin\Release\net9.0\STS2Mobile.GameIdentityTests.dll'
    dotnet $testAssembly --write-android-mono-fixture $fixture
    if ($LASTEXITCODE) { throw 'Android Mono fixture generation failed.' }
    & $ClangPath --target=aarch64-linux-android24 -std=c++17 -pthread -static-libstdc++ (Join-Path $fixture 'native-transition.cpp') -o (Join-Path $fixture 'native-transition') -ldl
    if ($LASTEXITCODE) { throw 'Android native fixture compilation failed.' }
    $deviceArguments = if ($DeviceSerial) { @('-s', $DeviceSerial) } else { @() }
    $remote = '/data/local/tmp/sts2-mono-transition-test'
    & $AdbPath @deviceArguments shell mkdir -p $remote
    if ($LASTEXITCODE) { throw 'Connected ARM64 Android device is required.' }
    $bcl = Join-Path $root 'android\assets\dotnet_bcl'
    $native = Join-Path $root 'android\libs\release\arm64-v8a'
    $inputs = @(Get-ChildItem -LiteralPath $bcl -Filter '*.dll' | Where-Object { $_.Name -match '^(System\.|Microsoft\.|mscorlib\.|netstandard\.)' } | ForEach-Object FullName)
    $inputs += @(Get-ChildItem -LiteralPath $native -Filter '*.so' | Where-Object { $_.Name -match '^lib(mono|System)' } | ForEach-Object FullName)
    $inputs += (Join-Path $fixture 'STS2Mobile.GameIdentityTests.dll'), (Join-Path $fixture 'native-transition')
    & $AdbPath @deviceArguments push @inputs "$remote/"
    if ($LASTEXITCODE) { throw 'Android Mono runtime fixture transfer failed.' }
    & $AdbPath @deviceArguments shell chmod 700 "$remote/native-transition"
    if ($LASTEXITCODE) { throw 'Android fixture permissions failed.' }
    $original = Join-Path $evidence 'android-mono-transition-original.log'
    & $AdbPath @deviceArguments shell "timeout -k 2 5 $remote/native-transition $remote" *> $original
    $originalExit = $LASTEXITCODE
    $originalLog = Get-Content -LiteralPath $original -Raw
    if ($originalExit -notin 124,137 -or $originalLog -notmatch 'ANDROID_MONO_GC_BEGIN' -or $originalLog -match 'ANDROID_MONO_GC_PASS') {
        throw "Original native-host GC wait was not reproduced. Inspect $original"
    }
    $repaired = Join-Path $evidence 'android-mono-transition-repaired.log'
    & $AdbPath @deviceArguments shell "timeout -k 2 15 $remote/native-transition $remote safe" *> $repaired
    if ($LASTEXITCODE -ne 0 -or (Get-Content -LiteralPath $repaired -Raw) -notmatch 'ANDROID_MONO_GC_PASS collections=64 callbacks=preserved') {
        throw "Native-host GC transition failed. Inspect $repaired"
    }
    Get-Content -LiteralPath $repaired
    Write-Host 'Scope: actual staged Android Mono runtime and callback/GC protocol; APK integration requires physical launch acceptance.'
    return
}
$godot = (Resolve-Path (Join-Path $root $GodotPath)).Path
$project = Join-Path $root 'tools\LauncherUiPreview'
$runtime = Join-Path $root 'upstream\godot-export\.godot\mono\publish\arm64'
$evidence = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $evidence | Out-Null

dotnet build (Join-Path $project 'LauncherUiPreview.csproj') -c Debug
if ($LASTEXITCODE -ne 0) { throw 'Godot resource probe build failed.' }

$arguments = @('--headless', '--path', $project, '--', '--asset-preload-test=true', "--managed-runtime-directory=$runtime")
if ($VerifyBaseline) {
    $baseline = Join-Path $evidence 'asset-preload-godot-baseline.log'
    & $godot @arguments '--unpatched=true' *> $baseline
    if ($LASTEXITCODE -eq 0 -or (Get-Content -Raw $baseline) -notmatch 'Threaded resource backlog exceeded its bound') {
        throw "The original game's resource burst was not reproduced. Inspect $baseline"
    }
    Write-Host 'Confirmed: the original game exceeds the Android resource backlog bound.'
}

$patched = Join-Path $evidence 'asset-preload-godot-patched.log'
& $godot @arguments *> $patched
if ($LASTEXITCODE -ne 0 -or (Get-Content -Raw $patched) -notmatch 'ASSET_PRELOAD_PASS' -or (Get-Content -Raw $patched) -notmatch 'ASSET_FALLBACK_PASS' -or (Get-Content -Raw $patched) -notmatch 'ASSET_OWNERSHIP_PASS') {
    throw "Patched resource loading failed. Inspect $patched"
}
Get-Content $patched | Select-String 'ASSET_PRELOAD_PASS|ASSET_FALLBACK_PASS|ASSET_OWNERSHIP_PASS'
Write-Host 'Scope: actual game AssetLoadingSession and Godot threaded resources on desktop; not Android gameplay.'
