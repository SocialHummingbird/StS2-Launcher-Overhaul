param(
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")

function Resolve-RepoPath([string]$RelativePath) {
    $normalized = $RelativePath -replace '[\\/]', [System.IO.Path]::DirectorySeparatorChar
    return Join-Path $root $normalized
}

$managedPath = Resolve-RepoPath "src\STS2Mobile\Steam\SteamGameBranch.cs"
$nativePath = Resolve-RepoPath "android\src\com\game\sts2launcher\SteamBranchInfo.java"

if (-not (Test-Path -LiteralPath $managedPath)) {
    throw "Missing managed branch guidance source: $managedPath"
}

if (-not (Test-Path -LiteralPath $nativePath)) {
    throw "Missing native branch guidance source: $nativePath"
}

$managed = Get-Content -LiteralPath $managedPath -Raw
$native = Get-Content -LiteralPath $nativePath -Raw

$requiredPhrases = @(
    "Default/public Steam branch",
    "Choose a game version from the dropdown",
    "Account-visible branch options refresh after Steam app-info is available",
    "beta password entry is still being hardened",
    "selected from the game version dropdown",
    "Private/password-protected branches may be inaccessible",
    "Failed downloads do not change Steam Cloud saves",
    "Save compatibility is unproven"
)

$failures = New-Object System.Collections.Generic.List[string]

foreach ($phrase in $requiredPhrases) {
    if ($managed -notlike "*$phrase*") {
        $failures.Add("Managed guidance missing phrase: $phrase")
    }

    if ($native -notlike "*$phrase*") {
        $failures.Add("Native guidance missing phrase: $phrase")
    }
}

if ($failures.Count -gt 0) {
    Write-Host "Steam branch guidance parity audit failed:"
    foreach ($failure in $failures) {
        Write-Host "FAIL $failure"
    }
    exit 1
}

if (-not $Quiet) {
    Write-Host "Steam branch guidance parity audit passed: $($requiredPhrases.Count) required phrases present in managed and native guidance."
}
