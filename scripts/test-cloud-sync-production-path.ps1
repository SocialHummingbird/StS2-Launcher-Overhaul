param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $root "tools\CloudSyncProductionPathProbe\CloudSyncProductionPathProbe.csproj"
$required = @(
    $project,
    (Join-Path $root "tools\CloudSyncProductionPathProbe\Program.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\InMemoryCloudSaveStore.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\AutomaticSyncScenarios.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\FilesystemAutomaticSyncScenarios.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\SaveRecoveryScenarios.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\Stage5ProcessDeathProbe.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\Stage5ProcessDeathStores.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Plan.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\AutomaticSyncResult.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Model.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Manifest.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Persistence.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Snapshots.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Reconcile.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.AutomaticSync.Retry.cs")
)

foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required non-mutating cloud validation source is missing: $path"
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "Could not locate dotnet. Install the .NET 9 SDK or add it to PATH."
}

Write-Host (
    "Running cloud sync production paths with filesystem fixtures, the " +
    "deterministic fake Steam store, and hard child-process restarts..."
)
& $dotnet.Source run `
    --project $project `
    --configuration Release `
    --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Non-mutating cloud sync production-path validation failed."
}

Write-Host (
    "Non-mutating desktop cloud validation passed. No credentials, network, " +
    "device, or real Steam Cloud operation was used."
)
