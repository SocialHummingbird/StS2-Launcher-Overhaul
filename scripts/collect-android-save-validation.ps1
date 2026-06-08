param(
    [string]$DeviceSerial = "",
    [string]$PackageName = "com.sts2launcher.overhaul.fork.local",
    [string]$AdbPath = "C:\Users\ap010\.w40k-android-toolchain\android-sdk\platform-tools\adb.exe",
    [string]$OutputRoot = "artifacts\android",
    [int]$WaitSeconds = 0,
    [int]$LogcatTailLines = 100000,
    [switch]$ClearLogcat,
    [switch]$EnableVerboseSaveDiagnostics,
    [switch]$DumpSaveFiles
)

$ErrorActionPreference = "Stop"

function Invoke-Adb {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Args)

    if ([string]::IsNullOrWhiteSpace($DeviceSerial)) {
        & $AdbPath @Args
    } else {
        & $AdbPath -s $DeviceSerial @Args
    }
}

if (-not (Test-Path -LiteralPath $AdbPath)) {
    throw "adb not found: $AdbPath"
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outDir = Join-Path $OutputRoot "save-validation-$timestamp"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

if ($EnableVerboseSaveDiagnostics) {
    Invoke-Adb shell run-as $PackageName sh -c "touch files/.sts2_verbose_save_diagnostics" | Out-Null
}

if ($ClearLogcat) {
    Invoke-Adb logcat -c | Out-Null
}

if ($WaitSeconds -gt 0) {
    Write-Host "Waiting $WaitSeconds seconds before collecting logcat..."
    Start-Sleep -Seconds $WaitSeconds
}

$properties = Invoke-Adb shell getprop | Out-String
$properties | Set-Content -LiteralPath (Join-Path $outDir "getprop.txt") -Encoding UTF8

$packageDump = Invoke-Adb shell dumpsys package $PackageName | Out-String
$packageDump | Set-Content -LiteralPath (Join-Path $outDir "package.txt") -Encoding UTF8

if ($LogcatTailLines -gt 0) {
    $rawLog = Invoke-Adb logcat -d -t $LogcatTailLines | Out-String
} else {
    $rawLog = Invoke-Adb logcat -d | Out-String
}
$rawLogPath = Join-Path $outDir "logcat.txt"
$rawLog | Set-Content -LiteralPath $rawLogPath -Encoding UTF8

$patterns = @(
    "STS2Mobile",
    "Assembly cache diagnostics",
    "Android startup freshness",
    "schema=",
    "New version detected",
    "re-copying all assemblies",
    "cache-hit",
    "expectedBytes",
    "expectedSource",
    "\[Cloud\]",
    "Candidate sync paths",
    "Enumerated cloud file sample",
    "Android local save",
    "Created Android local-only SaveManager",
    "Created SaveManager with SteamKit2 cloud store",
    "Pull complete",
    "complete with no downloads",
    "AndroidRuntime",
    "FATAL EXCEPTION"
)

$filtered = Select-String -LiteralPath $rawLogPath -Pattern $patterns -CaseSensitive:$false
$filtered | ForEach-Object { $_.Line } |
    Set-Content -LiteralPath (Join-Path $outDir "filtered-logcat.txt") -Encoding UTF8

$summary = [ordered]@{
    output = $outDir
    schema22Seen = [bool]($rawLog -match 'schema=22')
    startupFreshnessSeen = [bool]($rawLog -match 'Android startup freshness:.*schema=22')
    assemblyRecopySeen = [bool]($rawLog -match 'New version detected, re-copying all assemblies')
    assemblyCacheHitSeen = [bool]($rawLog -match 'Assembly cache diagnostics \[cache-hit\]')
    expectedBytesSeen = [bool]($rawLog -match 'expectedBytes=')
    pullAttemptSeen = [bool]($rawLog -match 'Pulling cloud saves to local|Pull complete|complete with no downloads|Android local save write:')
    oldPullConfirmationSeen = [bool]($rawLog -match 'Pull cloud saves to local\?')
    candidatePathsSeen = [bool]($rawLog -match '\[Cloud\] Candidate sync paths:')
    cloudSampleSeen = [bool]($rawLog -match '\[Cloud\] Enumerated cloud file sample:')
    localSaveBaseSeen = [bool]($rawLog -match '\[Cloud\] Android local save base:')
    localSaveWrites = ([regex]::Matches($rawLog, '\[Cloud\] Android local save write:')).Count
    localSaveReads = ([regex]::Matches($rawLog, '\[Cloud\] Android local save read')).Count
    localSaveExistsChecks = ([regex]::Matches($rawLog, '\[Cloud\] Android local save exists:')).Count
    localOnlySaveManagerSeen = [bool]($rawLog -match '\[Cloud\] Created Android local-only SaveManager')
    steamCloudSaveManagerSeen = [bool]($rawLog -match '\[Cloud\] Created SaveManager with SteamKit2 cloud store')
    pullNoDownloadsSeen = [bool]($rawLog -match 'complete with no downloads')
    pullWriteSeen = [bool]($rawLog -match '\[Cloud\] Pull wrote|\[Cloud\].*wrote .* bytes|\[Cloud\] Android local save write:')
    fatalExceptionSeen = [bool]($rawLog -match 'FATAL EXCEPTION|AndroidRuntime.*FATAL|AndroidRuntime.*Exception')
}

$summaryText = @(
    "Android save validation summary",
    "Output: $outDir",
    "schema=22 seen: $($summary.schema22Seen)",
    "startup freshness seen: $($summary.startupFreshnessSeen)",
    "assembly recopy seen: $($summary.assemblyRecopySeen)",
    "assembly cache-hit seen: $($summary.assemblyCacheHitSeen)",
    "expectedBytes seen: $($summary.expectedBytesSeen)",
    "pull attempt seen: $($summary.pullAttemptSeen)",
    "old pull confirmation seen: $($summary.oldPullConfirmationSeen)",
    "candidate paths seen: $($summary.candidatePathsSeen)",
    "cloud file sample seen: $($summary.cloudSampleSeen)",
    "local save base seen: $($summary.localSaveBaseSeen)",
    "local save writes: $($summary.localSaveWrites)",
    "local save reads: $($summary.localSaveReads)",
    "local save exists checks: $($summary.localSaveExistsChecks)",
    "local-only SaveManager seen: $($summary.localOnlySaveManagerSeen)",
    "Steam cloud SaveManager seen: $($summary.steamCloudSaveManagerSeen)",
    "pull no-downloads seen: $($summary.pullNoDownloadsSeen)",
    "pull write seen: $($summary.pullWriteSeen)",
    "fatal exception seen: $($summary.fatalExceptionSeen)"
)
$summaryText | Set-Content -LiteralPath (Join-Path $outDir "summary.txt") -Encoding UTF8

if ($DumpSaveFiles) {
    $allFiles = Invoke-Adb shell run-as $PackageName sh -c "find files -maxdepth 8 -type f 2>/dev/null"
    $saveFiles = $allFiles | Where-Object {
        $_ -match '\.save$' -or
        $_ -match '\.run$' -or
        $_ -match '\.bak$' -or
        $_ -match '/prefs$' -or
        $_ -match '/prefs\.save$'
    }

    $saveFiles | Set-Content -LiteralPath (Join-Path $outDir "save-files.txt") -Encoding UTF8
}

if ($EnableVerboseSaveDiagnostics) {
    Invoke-Adb shell run-as $PackageName sh -c "rm -f files/.sts2_verbose_save_diagnostics" | Out-Null
}

@{
    output = $outDir
    waitedSeconds = $WaitSeconds
    logcat = $rawLogPath
    filtered = (Join-Path $outDir "filtered-logcat.txt")
    summary = (Join-Path $outDir "summary.txt")
    package = (Join-Path $outDir "package.txt")
    saveFiles = if ($DumpSaveFiles) { (Join-Path $outDir "save-files.txt") } else { $null }
    gates = $summary
} | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $outDir "manifest.json") -Encoding UTF8

Write-Host "Saved Android save-validation evidence to $outDir"
