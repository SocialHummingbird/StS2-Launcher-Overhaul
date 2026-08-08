[CmdletBinding()]
param(
    [string]$DeviceSerial = $env:ANDROID_SERIAL,
    [string]$ApkPath = "",
    [string]$SourceCommit = "",
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)+$')]
    [string]$PackageName,
    [string]$AdbPath = "$(Join-Path $env:USERPROFILE '.w40k-android-toolchain\android-sdk\platform-tools\adb.exe')",
    [string]$OutputRoot = "artifacts\android",
    [Parameter(Mandatory = $true)]
    [ValidateSet("1", "2", "3", "4", "5", "6", "7", "8", "9", "10")]
    [string]$Stage5Row,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[a-z0-9][a-z0-9-]{0,63}$')]
    [string]$EvidencePhase,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}$')]
    [string]$LogcatSince,
    [ValidateRange(0, 86400)]
    [int]$WaitSeconds = 0
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$collector = Join-Path $scriptDir "collect-android-save-validation.ps1"

if (-not (Test-Path -LiteralPath $collector -PathType Leaf)) {
    throw "Collector script not found: $collector"
}

if ([string]::IsNullOrWhiteSpace($ApkPath)) {
    throw "-ApkPath is required so evidence is bound to the exact candidate APK. This script never builds or installs an APK."
}

$resolvedApk = Resolve-Path -LiteralPath $ApkPath -ErrorAction Stop
if (-not (Test-Path -LiteralPath $resolvedApk.Path -PathType Leaf) -or
    [IO.Path]::GetExtension($resolvedApk.Path) -ne ".apk") {
    throw "Candidate must be an existing APK file: $ApkPath"
}

if ($SourceCommit -notmatch '^[0-9a-fA-F]{40}$') {
    throw "-SourceCommit must be the full 40-character commit recorded by the candidate build."
}

$collectorArgs = @{
    DeviceSerial = $DeviceSerial
    ApkPath = $resolvedApk.Path
    SourceCommit = $SourceCommit.ToLowerInvariant()
    PackageName = $PackageName
    AdbPath = $AdbPath
    OutputRoot = $OutputRoot
    Stage5Row = $Stage5Row
    EvidencePhase = $EvidencePhase
    LogcatSince = $LogcatSince
    WaitSeconds = $WaitSeconds
}

Write-Host "Collecting read-only evidence for an already installed exact candidate."
Write-Host "This wrapper does not build, install, launch, force-stop, clear logcat, write a diagnostics marker, or trigger a Steam operation."
Write-Host "It records hashes and diagnostics; it is not a byte-for-byte save export."
& $collector @collectorArgs
