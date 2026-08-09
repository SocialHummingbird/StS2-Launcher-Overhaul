param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$project = Join-Path $root (
    "tools\LocalGameplaySaveSafetyProbe\LocalGameplaySaveSafetyProbe.csproj"
)
$upstreamAssembly = Join-Path $root (
    "upstream\godot-export\.godot\mono\publish\arm64\sts2.dll"
)
$required = @(
    $project,
    (Join-Path $root "tools\LocalGameplaySaveSafetyProbe\Program.cs"),
    $upstreamAssembly
)

foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required local-gameplay save-safety input is missing: $path"
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "Could not locate dotnet. Install the .NET 9 SDK or add it to PATH."
}

& $dotnet.Source run `
    --project $project `
    --configuration Release
if ($LASTEXITCODE -ne 0) {
    throw "Local gameplay save-safety validation failed."
}
