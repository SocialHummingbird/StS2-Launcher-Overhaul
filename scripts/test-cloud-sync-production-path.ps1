param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $root "tools\CloudSyncProductionPathProbe\CloudSyncProductionPathProbe.csproj"
$required = @(
    $project,
    (Join-Path $root "tools\CloudSyncProductionPathProbe\Program.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\InMemoryCloudSaveStore.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.ManualSync.Plan.cs")
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

Write-Host "Running cloud sync production paths with in-memory local/cloud stores..."
& $dotnet.Source run `
    --project $project `
    --configuration Release `
    --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Non-mutating cloud sync production-path validation failed."
}

Write-Host "Non-mutating cloud validation passed. No Steam Cloud Push was performed."
