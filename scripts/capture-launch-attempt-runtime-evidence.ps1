param(
    [string]$AdbPath = "adb",
    [string]$PackageName = "",
    [string]$DeviceSerial = "",
    [string]$OutputRoot = "artifacts\android",
    [string]$RunLabel = "launch-attempt",
    [int]$WaitForDeviceSeconds = 10,
    [switch]$RequirePublic,
    [switch]$RequirePublicBeta,
    [switch]$RequireBranchSwitch,
    [switch]$RequireSaveSafety,
    [switch]$RequireResolvedClassification,
    [switch]$IncludeRawLogcat,
    [int]$MaxLaunchAttemptAgeMinutes = 30,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-adb-utils.ps1")

$root = Split-Path -Parent $PSScriptRoot
$captureScript = Join-Path $PSScriptRoot "capture-multi-version-runtime-evidence.ps1"
$reviewScript = Join-Path $PSScriptRoot "review-multi-version-runtime-evidence.ps1"

if (-not (Test-Path -LiteralPath $captureScript)) {
    throw "Missing runtime evidence capture script: $captureScript"
}

if (-not (Test-Path -LiteralPath $reviewScript)) {
    throw "Missing runtime evidence review script: $reviewScript"
}

if ($WaitForDeviceSeconds -lt 0) {
    throw "-WaitForDeviceSeconds must be zero or greater."
}

if ($MaxLaunchAttemptAgeMinutes -lt 0) {
    throw "-MaxLaunchAttemptAgeMinutes must be zero or greater."
}

$branchRequirements = @($RequirePublic, $RequirePublicBeta, $RequireBranchSwitch) | Where-Object { $_.IsPresent }
if ($branchRequirements.Count -gt 1) {
    throw "Choose only one of -RequirePublic, -RequirePublicBeta, or -RequireBranchSwitch."
}

function Write-Status {
    param([string]$Message)

    if (-not $Quiet) {
        Write-Host $Message
    }
}

$AdbPath = Resolve-AndroidAdbPath -AdbPath $AdbPath
$DeviceSerial = Resolve-AndroidTargetDevice -AdbPath $AdbPath -DeviceSerial $DeviceSerial -WaitForDeviceSeconds $WaitForDeviceSeconds
$PackageName = Resolve-AndroidInstalledLauncherPackageName -AdbPath $AdbPath -DeviceSerial $DeviceSerial -PackageName $PackageName

$captureArguments = @(
    "-AdbPath", $AdbPath,
    "-PackageName", $PackageName,
    "-DeviceSerial", $DeviceSerial,
    "-OutputRoot", $OutputRoot,
    "-RunLabel", $RunLabel
)
if ($IncludeRawLogcat) {
    $captureArguments += "-IncludeRawLogcat"
}

Write-Status "Capturing launch-attempt runtime evidence for package $PackageName on $DeviceSerial"
$captureOutput = @(& $captureScript @captureArguments 2>&1)
$captureText = ($captureOutput | ForEach-Object { [string]$_ }) -join [Environment]::NewLine
$capturedPath = $null
foreach ($line in $captureOutput) {
    $text = [string]$line
    $match = [regex]::Match($text, "Multi-version runtime evidence captured:\s*(.+)$")
    if ($match.Success) {
        $capturedPath = $match.Groups[1].Value.Trim()
    }
}

if ([string]::IsNullOrWhiteSpace($capturedPath)) {
    throw "Runtime evidence capture completed but did not report an artifact path. Output:`n$captureText"
}

$resolvedEvidenceDir = (Resolve-Path -LiteralPath $capturedPath).ProviderPath

$reviewParameters = @{
    EvidenceDir = $resolvedEvidenceDir
    RequirePublic = $RequirePublic.IsPresent
    RequirePublicBeta = $RequirePublicBeta.IsPresent
    RequireBranchSwitch = $RequireBranchSwitch.IsPresent
    RequireSaveSafety = $RequireSaveSafety.IsPresent
    RequireLaunchAttempt = $true
    RequireResolvedClassification = $RequireResolvedClassification.IsPresent
    MaxLaunchAttemptAgeMinutes = $MaxLaunchAttemptAgeMinutes
}

if ($Quiet) {
    & $reviewScript @reviewParameters *> $null
} else {
    & $reviewScript @reviewParameters
}

Write-Status "Launch-attempt runtime evidence captured and reviewed: $resolvedEvidenceDir"
