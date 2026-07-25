param()

$ErrorActionPreference = "Stop"

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$sources = @(
    (Join-Path $root "src\STS2Mobile\Steam\CloudOperationState.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\ManualCloudSyncResult.cs"),
    (Join-Path $root "src\STS2Mobile\Steam\LocalBackupRefreshResult.cs"),
    (Join-Path $root "src\STS2Mobile\Launcher\CloudPushEligibilityResult.cs"),
    (Join-Path $root "src\STS2Mobile\Launcher\CloudPostOperationRefresh.cs"),
    (Join-Path $root "src\STS2Mobile\Launcher\CloudOperationTerminalPresentation.cs"),
    (Join-Path $root "src\STS2Mobile\Launcher\LocalBackupRecoveryPresentation.cs"),
    (Join-Path $root "scripts\tests\LauncherCloudPostOperationRefreshTest.cs")
)

foreach ($source in $sources) {
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Required cloud post-operation refresh test source is missing: $source"
    }
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    throw "Could not locate dotnet. Install the .NET 9 SDK or add it to PATH."
}

$tempRoot = [System.IO.Path]::GetFullPath(
    [System.IO.Path]::GetTempPath()
)
$output = [System.IO.Path]::GetFullPath(
    (Join-Path $tempRoot (
        "sts2-cloud-post-operation-refresh-" + [Guid]::NewGuid().ToString("N")
    ))
)
if (-not $output.StartsWith(
    $tempRoot,
    [System.StringComparison]::OrdinalIgnoreCase
)) {
    throw "Refusing to use test output outside the system temporary directory: $output"
}

$compileItems = $sources |
    ForEach-Object {
        $escaped = [System.Security.SecurityElement]::Escape($_)
        "    <Compile Include=`"$escaped`" />"
    }
$projectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AssemblyName>LauncherCloudPostOperationRefreshTest</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
$($compileItems -join [Environment]::NewLine)
  </ItemGroup>
</Project>
"@

try {
    New-Item -ItemType Directory -Force -Path $output | Out-Null
    $projectPath = Join-Path $output "CloudPostOperationRefreshTests.csproj"
    [System.IO.File]::WriteAllText(
        $projectPath,
        $projectXml,
        [System.Text.UTF8Encoding]::new($false)
    )

    & $dotnet.Source run `
        --project $projectPath `
        --configuration Release `
        --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Launcher cloud post-operation refresh tests failed."
    }
} finally {
    if (Test-Path -LiteralPath $output -PathType Container) {
        Remove-Item -LiteralPath $output -Recurse -Force
    }
}
