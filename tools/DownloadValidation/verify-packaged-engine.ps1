param([Parameter(Mandatory)][string]$ValidationApk, [Parameter(Mandatory)][string]$NormalApk)
$ErrorActionPreference = 'Stop'
Add-Type -Path "$PSScriptRoot/../SteamKitAndroidPatch/bin/Release/net9.0/Mono.Cecil.dll"
function Read-Engine([string]$path) {
    $zip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $path))
    $bytes = [IO.MemoryStream]::new()
    try {
        $entry = $zip.GetEntry('assets/dotnet_bcl/STS2Mobile.dll')
        if (-not $entry) { throw 'Packaged launcher assembly missing' }
        $stream = $entry.Open()
        try { $stream.CopyTo($bytes) } finally { $stream.Dispose() }
    } finally { $zip.Dispose() }
    $bytes.Position = 0
    $assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($bytes)
    try {
        $types = @($assembly.MainModule.GetTypes())
        $il = @(
            foreach ($type in ($types | Where-Object { $_.FullName -like 'STS2Mobile.Steam.DepotChunkTransfer*' -or $_.FullName -like 'STS2Mobile.Steam.DepotDownloadStaging*' -or $_.FullName -eq 'STS2Mobile.Steam.DepotChunkDonor' } | Sort-Object FullName)) {
                $type.FullName
                foreach ($method in ($type.Methods | Sort-Object FullName)) {
                    $method.FullName
                    if ($method.HasBody) {
                        foreach ($instruction in $method.Body.Instructions) { $instruction.ToString() }
                        foreach ($local in $method.Body.Variables) { $local.VariableType.FullName }
                    }
                }
            }
        ) -join "`n"
        [pscustomobject]@{
            assemblySha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes.ToArray()))
            engineIlSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($il)))
            hasValidationHarness = @($types | Where-Object FullName -Like '*DownloadValidation*').Count -gt 0
            hasStagingInspector = @($types | Where-Object FullName -Like 'STS2Mobile.Launcher.ValidationStag*').Count -gt 0
            hasValidationPathOverride = @($types | ForEach-Object Fields | Where-Object Name -eq 'ValidationDataRoot').Count -gt 0
        }
    } finally { $assembly.Dispose(); $bytes.Dispose() }
}
$validation = Read-Engine $ValidationApk
$normal = Read-Engine $NormalApk
if (-not $validation.hasValidationHarness -or $normal.hasValidationHarness -or $normal.hasStagingInspector -or $normal.hasValidationPathOverride) { throw 'Validation build separation failed' }
if ($validation.engineIlSha256 -ne $normal.engineIlSha256) { throw 'Packaged transfer-engine IL differs between tested and normal APK' }
function Read-ZipBytes($zip, [string]$entryName) {
    $stream = $zip.GetEntry($entryName).Open()
    $bytes = [IO.MemoryStream]::new()
    try { $stream.CopyTo($bytes); return ,$bytes.ToArray() } finally { $stream.Dispose(); $bytes.Dispose() }
}
$testZip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $ValidationApk))
$normalZip = [IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $NormalApk))
try {
    $dependencyMatches = @{}
    foreach ($name in @('SteamKit2.dll','System.Net.WebSockets.Client.dll','ZstdSharp.dll')) {
        $testHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData((Read-ZipBytes $testZip "assets/dotnet_bcl/$name")))
        $normalHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData((Read-ZipBytes $normalZip "assets/dotnet_bcl/$name")))
        $dependencyMatches[$name] = $testHash -eq $normalHash
        if ($testHash -ne $normalHash) { throw "Packaged dependency differs: $name" }
    }
    $dex = ($normalZip.Entries | Where-Object FullName -Like '*.dex' | ForEach-Object { [Text.Encoding]::UTF8.GetString((Read-ZipBytes $normalZip $_.FullName)) }) -join ''
    foreach ($marker in @('DownloadValidationHttpDiagnostics','configureDownloadValidationHttp','replayDownloadValidationResource')) {
        if ($dex.Contains($marker)) { throw "Normal APK retains a validation Java marker: $marker. Clean the generated Gradle output and rebuild." }
    }
} finally { $testZip.Dispose(); $normalZip.Dispose() }
[pscustomobject]@{ validation=$validation; normal=$normal; engineMatches=$true; normalHasNoHarness=$true; normalJavaHooksAbsent=$true; dependencyMatches=$dependencyMatches } | ConvertTo-Json -Depth 5
