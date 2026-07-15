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
    @{ Fixture = "ready"; Destination = "home"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "saves"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "versions"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "mods"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "help"; Width = 2400; Height = 1080; Touch = $true },
    @{ Fixture = "ready"; Destination = "home"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "ready"; Destination = "saves"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "ready"; Destination = "versions"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "ready"; Destination = "mods"; Width = 2184; Height = 1968; Touch = $true },
    @{ Fixture = "ready"; Destination = "help"; Width = 2184; Height = 1968; Touch = $true }
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

Write-Host "Launcher UI preview matrix passed: $($cases.Count) screenshots; deterministic SHA256 $baselineHash; event contract passed"
