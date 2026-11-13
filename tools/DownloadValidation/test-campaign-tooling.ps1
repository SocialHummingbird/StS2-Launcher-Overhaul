param()
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
$output = Join-Path $repo 'tmp/cdn-validation-20261113/generator-test'
$source = Join-Path $repo 'android/src/com/game/sts2launcher/GodotApp.java'
$before = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
if (-not (Test-Path -LiteralPath "$PSScriptRoot/prepare-java.ps1")) { throw 'Missing validation-only Java timing generator.' }
& "$PSScriptRoot/prepare-java.ps1" -OutputDirectory $output
$generated = Get-Content -LiteralPath "$output/com/game/sts2launcher/GodotApp.java" -Raw
$generated += Get-Content -LiteralPath "$output/com/game/sts2launcher/DownloadValidationHttpDiagnostics.java" -Raw
foreach ($marker in @('workerEnter','workerExit','connection-start','headers','body-stream','disconnect-start','request-cancelled','replayDownloadValidationResource')) {
    if (-not $generated.Contains($marker)) { throw "Missing timing boundary: $marker" }
}
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $before) { throw 'Generator changed production Java source.' }
$build = [Management.Automation.Language.Parser]::ParseFile("$repo/scripts/build-android-local.ps1", [ref]$null, [ref]$null)
if ('ValidationGradleInitScript' -notin $build.ParamBlock.Parameters.Name.VariablePath.UserPath) { throw 'Build wrapper has no explicit validation-only Java input.' }
$sandboxPath = "$PSScriptRoot/SandboxDownloadValidation.cs"
if (-not (Test-Path -LiteralPath $sandboxPath)) { throw 'Missing opt-in sandbox UI validation.' }
$sandbox = Get-Content -LiteralPath $sandboxPath -Raw
foreach ($forbidden in @('.StartSession(', '.StartSessionFlow(', '.LaunchPressed(', '.SaveGameBranch(')) {
    if ($sandbox.Contains($forbidden)) { throw "Sandbox binds a forbidden normal-session action: $forbidden" }
}
foreach ($required in @('LauncherDownloadCoordinator(', 'WireValidationDownloadEvents(', 'ValidationDataRoot = sandbox', 'SourceRunId', 'VerifyValidationFiles(')) {
    if (-not $sandbox.Contains($required)) { throw "Sandbox is missing its required boundary: $required" }
}
Write-Output 'PASS generated timing boundaries, unchanged production Java and explicit validation build input.'
