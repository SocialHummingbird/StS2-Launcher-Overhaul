param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $root "tools\LocalSaveRecoveryProductionPathProbe\LocalSaveRecoveryProductionPathProbe.csproj"
$required = @(
    $project,
    (Join-Path $root "tools\LocalSaveRecoveryProductionPathProbe\Program.cs"),
    (Join-Path $root "tools\CloudSyncProductionPathProbe\InMemoryCloudSaveStore.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\CloudSyncCoordinator.SaveBackups.LocalMirror.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\AndroidLocalSaveStore.FileIo.cs")
)

foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required local-save backup-only validation source is missing: $path"
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "Could not locate dotnet. Install the .NET 9 SDK or add it to PATH."
}

Write-Host "Running local-save backup-only production paths with temporary trees and in-memory stores..."
& $dotnet.Source run `
    --project $project `
    --configuration Release `
    --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Non-mutating local-save backup-only production-path validation failed."
}

Write-Host "Local-save backup-only validation passed. No automatic restore or Steam Cloud operation was performed."
