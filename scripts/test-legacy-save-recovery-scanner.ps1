param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $root "tools\LegacySaveRecoveryScannerProbe\LegacySaveRecoveryScannerProbe.csproj"
$required = @(
    $project,
    (Join-Path $root "tools\LegacySaveRecoveryScannerProbe\Program.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\InMemoryCloudSaveStore.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.Recovery.Legacy.Model.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.Recovery.Legacy.Scanner.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.Recovery.Legacy.Sources.Local.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.Recovery.Legacy.Sources.External.cs")
)

foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required legacy recovery scanner source is missing: $path"
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "Could not locate dotnet. Install the .NET 9 SDK or add it to PATH."
}

Write-Host "Running bounded legacy recovery scan/import scenarios with fake local and temporary external stores..."
& $dotnet.Source run `
    --project $project `
    --configuration Release `
    --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Legacy save recovery scanner validation failed."
}

Write-Host "Legacy recovery scanner validation passed. Source saves were read-only and no Steam operation was used."
