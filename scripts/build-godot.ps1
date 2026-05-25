param(
    [string]$GodotDir = $(if ($env:GODOT_DIR) { $env:GODOT_DIR } else { Join-Path $PSScriptRoot "..\vendor\godot" }),
    [string[]]$Arches = $(if ($env:ARCHES) { $env:ARCHES -split "\s+" } else { @("arm64", "x86_64") }),
    [string]$ExpectedGodotVersion = $(if ($env:EXPECTED_GODOT_VERSION) { $env:EXPECTED_GODOT_VERSION } else { "4.5.1-stable" }),
    [ValidateSet("template_release", "template_debug")]
    [string]$Target = $(if ($env:GODOT_TARGET) { $env:GODOT_TARGET } else { "template_release" }),
    [switch]$DebugSymbols,
    [switch]$VisibleSymbols,
    [int]$Jobs = $(if ($env:JOBS) { [int]$env:JOBS } else { [int]$env:NUMBER_OF_PROCESSORS })
)

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([System.IO.Path]::IsPathRooted($GodotDir)) {
    $GodotDir = [System.IO.Path]::GetFullPath($GodotDir)
} else {
    $GodotDir = [System.IO.Path]::GetFullPath((Join-Path $root $GodotDir))
}
$androidLibs = Join-Path $root "android\libs\release"
$venvPython = Join-Path $root "venv\Scripts\python.exe"
$scons = Join-Path $root "venv\Scripts\scons.exe"

function Apply-GodotPatches {
    param(
        [string]$GodotDir,
        [string]$Root
    )

    $patchDir = Join-Path $Root "patches\godot"
    if (-not (Test-Path -LiteralPath $patchDir)) {
        return
    }

    $patches = Get-ChildItem -LiteralPath $patchDir -Filter "*.patch" | Sort-Object Name
    foreach ($patch in $patches) {
        Push-Location $GodotDir
        try {
            & git apply --check --ignore-whitespace $patch.FullName *> $null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Applying Godot patch: $($patch.Name)"
                & git apply --ignore-whitespace $patch.FullName
                if ($LASTEXITCODE -ne 0) {
                    throw "Failed to apply Godot patch: $($patch.FullName)"
                }
                continue
            }

            & git apply --reverse --check --ignore-whitespace $patch.FullName *> $null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Godot patch already applied: $($patch.Name)"
                continue
            }

            throw "Godot patch cannot be applied cleanly: $($patch.FullName)"
        } finally {
            Pop-Location
        }
    }
}

if (-not (Test-Path -LiteralPath $GodotDir)) {
    throw "Expected Godot source checkout at $GodotDir. Run .\scripts\setup-godot-source.ps1, or set GODOT_DIR to the custom patched Godot checkout."
}

Apply-GodotPatches -GodotDir $GodotDir -Root $root

if (-not (Test-Path -LiteralPath $venvPython) -or -not (Test-Path -LiteralPath $scons)) {
    throw "Expected Python virtualenv with SCons at $root\venv. Run .\scripts\setup-godot-source.ps1 first."
}

if (-not $env:ANDROID_HOME) {
    $candidates = @(
        (Join-Path $HOME ".w40k-android-toolchain\android-sdk"),
        (Join-Path $HOME "AppData\Local\Android\Sdk")
    )
    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            $env:ANDROID_HOME = $candidate
            break
        }
    }
}

if (-not $env:ANDROID_HOME) {
    throw "ANDROID_HOME is not set and no default Android SDK path was found."
}

if (-not $env:ANDROID_NDK_ROOT) {
    $ndkDir = Join-Path $env:ANDROID_HOME "ndk"
    $latestNdk = Get-ChildItem -LiteralPath $ndkDir -Directory | Sort-Object Name | Select-Object -Last 1
    if (-not $latestNdk) {
        throw "No Android NDK found under $ndkDir."
    }
    $env:ANDROID_NDK_ROOT = $latestNdk.FullName
}

$versionPy = Join-Path $GodotDir "version.py"
if (Test-Path -LiteralPath $versionPy) {
    Push-Location $GodotDir
    try {
        $versionExpr = "import runpy; s=runpy.run_path('version.py'); print(str(s.get('major'))+'.'+str(s.get('minor'))+'.'+str(s.get('patch'))+'-'+str(s.get('status')))"
        $actualVersion = (& $venvPython -c $versionExpr 2>&1)
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to read Godot version.py: $actualVersion"
        }
    } finally {
        Pop-Location
    }
    $actualVersion = ($actualVersion | Select-Object -Last 1).Trim()
    if ($actualVersion -ne $ExpectedGodotVersion) {
        throw "Godot checkout is $actualVersion, expected $ExpectedGodotVersion. Set EXPECTED_GODOT_VERSION to override intentionally."
    }
}

$aarPath = Join-Path $androidLibs "godot-lib.template_release.aar"
$hasAar = Test-Path -LiteralPath $aarPath
if ($hasAar) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
} else {
    Write-Host "No local Godot AAR found at $aarPath; will update android/libs/release JNI .so files only."
}

foreach ($arch in $Arches) {
    switch ($arch) {
        "arm64" { $abi = "arm64-v8a" }
        "x86_64" { $abi = "x86_64" }
        default { throw "Unsupported Android Godot arch: $arch" }
    }

    $sconsArgs = @(
        "platform=android",
        "arch=$arch",
        "target=$Target",
        "module_mono_enabled=yes",
        "-j$Jobs"
    )
    if ($DebugSymbols) {
        $sconsArgs += "debug_symbols=yes"
        $sconsArgs += "separate_debug_symbols=yes"
    }
    if ($VisibleSymbols) {
        $sconsArgs += "symbols_visibility=visible"
    }

    Write-Host "Building Godot (android $arch $Target)..."
    Push-Location $GodotDir
    try {
        & $scons @sconsArgs
        if ($LASTEXITCODE -ne 0) {
            throw "SCons failed for $arch."
        }
    } finally {
        Pop-Location
    }

    $buildTypeDir = if ($Target -eq "template_debug") { "debug" } else { "release" }
    $builtSo = Join-Path $GodotDir "platform\android\java\lib\libs\$buildTypeDir\$abi\libgodot_android.so"
    if (-not (Test-Path -LiteralPath $builtSo)) {
        throw "Expected output not found at $builtSo."
    }

    Write-Host "Updating libgodot_android.so for $abi..."
    New-Item -ItemType Directory -Force -Path (Join-Path $androidLibs $abi) | Out-Null
    Copy-Item -LiteralPath $builtSo -Destination (Join-Path $androidLibs "$abi\libgodot_android.so") -Force

    if ($hasAar) {
        $zip = [System.IO.Compression.ZipFile]::Open($aarPath, [System.IO.Compression.ZipArchiveMode]::Update)
        try {
            $entryName = "jni/$abi/libgodot_android.so"
            $existing = $zip.GetEntry($entryName)
            if ($existing) {
                $existing.Delete()
            }
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $builtSo, $entryName) | Out-Null
        } finally {
            $zip.Dispose()
        }
    }
}

Write-Host "Godot engine rebuild complete for: $($Arches -join ' ')"
