param(
    [string]$GodotPath = "",
    [ValidateSet(
        "signed-out",
        "guard",
        "download",
        "ready",
        "error"
    )]
    [string]$Fixture = "ready",
    [ValidateSet("home", "saves", "versions", "mods", "help")]
    [string]$Destination = "home",
    [int]$Width = 1280,
    [int]$Height = 800,
    [bool]$TouchOptimized = $true,
    [string]$OutputPath = "",
    [switch]$SkipBuild,
    [switch]$InteractionTest,
    [switch]$ModRuntimeTest,
    [ValidateSet("active", "vanilla", "disabled", "stage9-chain")]
    [string]$ModRuntimeScenario = "active",
    [string]$BaseLibRoot = "",
    [string]$ImportVanillaSavesRoot = "",
    [string]$BaseGamePckPath = "",
    [string]$SteamworksNetPath = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "tools\LauncherUiPreview"
$defaultGodot = Join-Path $root (
    "tmp\godot-4.5.1-mono\runtime\Godot_v4.5.1-stable_mono_win64\" +
    "Godot_v4.5.1-stable_mono_win64_console.exe"
)

if (-not $GodotPath) {
    $GodotPath = $defaultGodot
}
if (-not (Test-Path -LiteralPath $GodotPath -PathType Leaf)) {
    throw "Godot 4.5.1 Mono was not found: $GodotPath"
}
if ($InteractionTest -and $OutputPath) {
    throw "OutputPath cannot be used with InteractionTest."
}
if ($ModRuntimeTest -and $InteractionTest) {
    throw "ModRuntimeTest and InteractionTest must run in separate Godot processes."
}

if (-not $ModRuntimeTest -and -not [string]::IsNullOrWhiteSpace($ImportVanillaSavesRoot)) {
    throw "Explicit mod roots require ModRuntimeTest."
}
if (-not $ModRuntimeTest -and -not [string]::IsNullOrWhiteSpace($BaseLibRoot)) {
    throw "Explicit mod roots require ModRuntimeTest."
}
if (-not $ModRuntimeTest -and $SteamworksNetPath) {
    throw "SteamworksNetPath is only used by ModRuntimeTest."
}
if (-not $ModRuntimeTest -and $BaseGamePckPath) {
    throw "BaseGamePckPath is only used by ModRuntimeTest."
}

$resolvedImportVanillaSavesRoot = ""
$resolvedBaseLibRoot = ""
$resolvedBaseGamePckPath = ""
$resolvedSteamworksNetPath = ""
$resolvedManagedRuntimeDirectory = ""
$runtimeDataDir = ""
if ($ModRuntimeTest) {
    if ([string]::IsNullOrWhiteSpace($ImportVanillaSavesRoot)) {
        throw "ModRuntimeTest requires an explicit ImportVanillaSavesRoot path."
    }
    if ([string]::IsNullOrWhiteSpace($SteamworksNetPath) -or
        -not (Test-Path -LiteralPath $SteamworksNetPath -PathType Leaf)) {
        throw "ModRuntimeTest requires an explicit SteamworksNetPath file."
    }
    if ([string]::IsNullOrWhiteSpace($BaseGamePckPath) -or
        -not (Test-Path -LiteralPath $BaseGamePckPath -PathType Leaf)) {
        throw "ModRuntimeTest requires an explicit BaseGamePckPath file."
    }

    if (-not (Test-Path -LiteralPath $ImportVanillaSavesRoot -PathType Container)) {
        throw "Explicit ImportVanillaSaves root was not found: $ImportVanillaSavesRoot"
    }
    if ($ModRuntimeScenario -eq "stage9-chain") {
        if ([string]::IsNullOrWhiteSpace($BaseLibRoot) -or
            -not (Test-Path -LiteralPath $BaseLibRoot -PathType Container)) {
            throw "The stage9-chain runtime test requires an explicit BaseLibRoot directory."
        }
        $resolvedBaseLibRoot = (Resolve-Path -LiteralPath $BaseLibRoot).Path
    }
    elseif (-not [string]::IsNullOrWhiteSpace($BaseLibRoot)) {
        throw "BaseLibRoot is only used by the stage9-chain runtime scenario."
    }

    $resolvedImportVanillaSavesRoot = (Resolve-Path -LiteralPath $ImportVanillaSavesRoot).Path
    $resolvedBaseGamePckPath = (Resolve-Path -LiteralPath $BaseGamePckPath).Path
    $resolvedSteamworksNetPath = (Resolve-Path -LiteralPath $SteamworksNetPath).Path
    $managedRuntimeDirectory = Split-Path -Parent $resolvedSteamworksNetPath
    if (-not (Test-Path -LiteralPath $managedRuntimeDirectory -PathType Container)) {
        throw "The managed-runtime directory containing Steamworks.NET.dll was not found."
    }
    $resolvedManagedRuntimeDirectory = (Resolve-Path -LiteralPath $managedRuntimeDirectory).Path
    $runtimeDataDir = Join-Path (
        [System.IO.Path]::GetTempPath()
    ) ("sts2-launcher-mod-runtime-" + [Guid]::NewGuid().ToString("N"))
}
elseif ($InteractionTest) {
    $runtimeDataDir = Join-Path (
        [System.IO.Path]::GetTempPath()
    ) ("sts2-launcher-ui-interaction-" + [Guid]::NewGuid().ToString("N"))
}

$resolvedOutput = ""
if (-not $InteractionTest -and -not $ModRuntimeTest) {
    if (-not $OutputPath) {
        $OutputPath = Join-Path $root (
            "artifacts\ui-preview\$Fixture-$Destination-$($Width)x$($Height).png"
        )
    }
    $resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
        $OutputPath
    )
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $resolvedOutput) | Out-Null
}

if (-not $SkipBuild) {
    dotnet build (Join-Path $project "LauncherUiPreview.csproj") -c Debug
    if ($LASTEXITCODE -ne 0) {
        throw "Launcher UI preview build failed"
    }
}

$touch = if ($TouchOptimized) { "true" } else { "false" }
$interaction = if ($InteractionTest) { "true" } else { "false" }
$runtime = if ($ModRuntimeTest) { "true" } else { "false" }
$godotArguments = @("--quiet")
if ($ModRuntimeTest) {
    $godotArguments += "--headless"
}
$godotArguments += @(
    "--path",
    $project,
    "--",
    "--fixture=$Fixture",
    "--destination=$Destination",
    "--width=$Width",
    "--height=$Height",
    "--touch=$touch",
    "--interaction-test=$interaction",
    "--mod-runtime-test=$runtime",
    "--mod-runtime-scenario=$ModRuntimeScenario",
    "--runtime-data-dir=$runtimeDataDir",
    "--base-lib-root=$resolvedBaseLibRoot",
    "--import-vanilla-saves-root=$resolvedImportVanillaSavesRoot",
    "--base-game-pck-path=$resolvedBaseGamePckPath",
    "--managed-runtime-directory=$resolvedManagedRuntimeDirectory",
    "--steamworks-net-path=$resolvedSteamworksNetPath",
    "--output=$resolvedOutput"
)

$modRuntimeOriginalAppData = [System.Environment]::GetEnvironmentVariable(
    "APPDATA",
    [System.EnvironmentVariableTarget]::Process
)
$modRuntimeAppDataOverridden = $false
$originalPreviewDataDir = [System.Environment]::GetEnvironmentVariable(
    "STS2_LAUNCHER_PREVIEW_DATA_DIR",
    [System.EnvironmentVariableTarget]::Process
)
$previewDataDirOverridden = $false
try {
    if ($runtimeDataDir) {
        New-Item -ItemType Directory -Path $runtimeDataDir | Out-Null
    }
    if ($ModRuntimeTest) {
        [System.Environment]::SetEnvironmentVariable(
            "APPDATA",
            $runtimeDataDir,
            [System.EnvironmentVariableTarget]::Process
        )
        $modRuntimeAppDataOverridden = $true
    }
    if ($InteractionTest) {
        [System.Environment]::SetEnvironmentVariable(
            "STS2_LAUNCHER_PREVIEW_DATA_DIR",
            $runtimeDataDir,
            [System.EnvironmentVariableTarget]::Process
        )
        $previewDataDirOverridden = $true
    }
    & $GodotPath @godotArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Launcher UI preview process failed"
    }
}
finally {
    if ($modRuntimeAppDataOverridden) {
        [System.Environment]::SetEnvironmentVariable(
            "APPDATA",
            $modRuntimeOriginalAppData,
            [System.EnvironmentVariableTarget]::Process
        )
    }
    if ($previewDataDirOverridden) {
        [System.Environment]::SetEnvironmentVariable(
            "STS2_LAUNCHER_PREVIEW_DATA_DIR",
            $originalPreviewDataDir,
            [System.EnvironmentVariableTarget]::Process
        )
    }
    if ($runtimeDataDir) {
        $resolvedRuntimeDataDir = [System.IO.Path]::GetFullPath($runtimeDataDir)
        $resolvedTempRoot = [System.IO.Path]::GetFullPath(
            [System.IO.Path]::GetTempPath()
        ).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
        $isModRuntimeDir = $resolvedRuntimeDataDir.StartsWith(
            $resolvedTempRoot + "sts2-launcher-mod-runtime-",
            [System.StringComparison]::OrdinalIgnoreCase
        )
        $isInteractionDir = $resolvedRuntimeDataDir.StartsWith(
            $resolvedTempRoot + "sts2-launcher-ui-interaction-",
            [System.StringComparison]::OrdinalIgnoreCase
        )
        if (-not $isModRuntimeDir -and -not $isInteractionDir) {
            throw "Refusing to remove unexpected runtime data directory: $resolvedRuntimeDataDir"
        }

        if (Test-Path -LiteralPath $resolvedRuntimeDataDir -PathType Container) {
            Remove-Item -LiteralPath $resolvedRuntimeDataDir -Recurse -Force
        }
    }

}

if ($InteractionTest) {
    Write-Host "Launcher UI interaction test passed."
    return
}

if ($ModRuntimeTest) {
    Write-Host "Desktop mod runtime fixture passed ($ModRuntimeScenario); Android activation and in-game effects remain unproven."
    return
}

if (-not (Test-Path -LiteralPath $resolvedOutput -PathType Leaf)) {
    throw "Launcher UI preview did not create a screenshot: $resolvedOutput"
}

Write-Host "Launcher UI preview: $resolvedOutput"
