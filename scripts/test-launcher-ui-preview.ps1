param(
    [string]$GodotPath = ""
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "tools\LauncherUiPreview\LauncherUiPreview.csproj"
$runner = Join-Path $PSScriptRoot "run-launcher-ui-preview.ps1"

dotnet build $project -c Debug
if ($LASTEXITCODE -ne 0) {
    throw "Launcher UI preview matrix build failed"
}

$cases = @(
    @{ Fixture = "signed-out"; Destination = "home"; Width = 1080; Height = 2400; Touch = $true },
    @{ Fixture = "guard"; Destination = "home"; Width = 1080; Height = 2400; Touch = $true },
    @{ Fixture = "download"; Destination = "home"; Width = 1080; Height = 2400; Touch = $true },
    @{ Fixture = "error"; Destination = "home"; Width = 1080; Height = 2400; Touch = $true },
    @{ Fixture = "ready"; Destination = "home"; Width = 1080; Height = 2400; Touch = $true },
    @{ Fixture = "ready"; Destination = "home"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "ready"; Destination = "saves"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "ready"; Destination = "versions"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "ready"; Destination = "mods"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "ready"; Destination = "help"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "pull-transfer"; Destination = "saves"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "pull-complete"; Destination = "saves"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "sync-source-choice"; Destination = "home"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "sync-reconciling"; Destination = "home"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "sync-conflict"; Destination = "home"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "sync-offline-pending"; Destination = "home"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "recovery-empty"; Destination = "saves"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "recovery-unknown"; Destination = "saves"; Width = 1080; Height = 2400; Touch = $true },
    @{ Fixture = "recovery-confirm"; Destination = "saves"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "recovery-restored"; Destination = "saves"; Width = 1280; Height = 800; Touch = $false },
    @{ Fixture = "sync-source-choice"; Destination = "home"; Width = 1080; Height = 2400; Touch = $true },
    @{ Fixture = "ready"; Destination = "home"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "saves"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "versions"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "mods"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "help"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "pull-transfer"; Destination = "saves"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "home"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "ready"; Destination = "saves"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "ready"; Destination = "versions"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "ready"; Destination = "mods"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "ready"; Destination = "help"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "pull-transfer"; Destination = "saves"; Width = 1080; Height = 2400; Touch = $true },
    @{ Fixture = "pull-complete"; Destination = "saves"; Width = 1080; Height = 2400; Touch = $true }
)

foreach ($case in $cases) {
    $arguments = @{
        Fixture = $case.Fixture
        Destination = $case.Destination
        Width = $case.Width
        Height = $case.Height
        TouchOptimized = $case.Touch
        SkipBuild = $true
    }
    if ($GodotPath) {
        $arguments.GodotPath = $GodotPath
    }
    & $runner @arguments
}

$baseline = Join-Path $root "artifacts\ui-preview\ready-versions-1280x800.png"
$repeat = Join-Path $root "artifacts\ui-preview\determinism-ready-versions-1280x800.png"
$repeatArguments = @{
    Fixture = "ready"
    Destination = "versions"
    Width = 1280
    Height = 800
    TouchOptimized = $false
    OutputPath = $repeat
    SkipBuild = $true
}
if ($GodotPath) {
    $repeatArguments.GodotPath = $GodotPath
}
& $runner @repeatArguments

$baselineHash = (Get-FileHash -LiteralPath $baseline -Algorithm SHA256).Hash
$repeatHash = (Get-FileHash -LiteralPath $repeat -Algorithm SHA256).Hash
if ($baselineHash -ne $repeatHash) {
    throw "Launcher UI preview is not deterministic: $baselineHash != $repeatHash"
}

$syncBaseline = Join-Path $root "artifacts\ui-preview\sync-source-choice-home-1280x800.png"
$syncRepeat = Join-Path $root "artifacts\ui-preview\determinism-sync-source-choice-home-1280x800.png"
$syncRepeatArguments = @{
    Fixture = "sync-source-choice"
    Destination = "home"
    Width = 1280
    Height = 800
    TouchOptimized = $false
    OutputPath = $syncRepeat
    SkipBuild = $true
}
if ($GodotPath) {
    $syncRepeatArguments.GodotPath = $GodotPath
}
& $runner @syncRepeatArguments

$syncBaselineHash = (Get-FileHash -LiteralPath $syncBaseline -Algorithm SHA256).Hash
$syncRepeatHash = (Get-FileHash -LiteralPath $syncRepeat -Algorithm SHA256).Hash
if ($syncBaselineHash -ne $syncRepeatHash) {
    throw "Automatic-sync UI preview is not deterministic: $syncBaselineHash != $syncRepeatHash"
}

$contractArguments = @{
    Fixture = "ready"
    Destination = "home"
    Width = 1280
    Height = 800
    TouchOptimized = $false
    OutputPath = (Join-Path $root "artifacts\ui-preview\contract-ready-1280x800.png")
    ValidateContract = $true
    SkipBuild = $true
}
if ($GodotPath) {
    $contractArguments.GodotPath = $GodotPath
}
& $runner @contractArguments

Write-Host "Launcher UI preview matrix passed: $($cases.Count) screenshots; deterministic SHA256 $baselineHash; automatic-sync SHA256 $syncBaselineHash; event contract passed"
